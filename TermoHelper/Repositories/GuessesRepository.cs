namespace TermoHelper.Repositories
{
    public enum GuessOutcome
    {
        NotPresent,
        Present,
        Correct
    };

    public class GuessesRepository
    {
        private List<string> dictionary;
        private List<string> answers;
        private List<Tuple<string, int>> bestGuesses;
        private Trie tree;
        List<char> lettersCorrect;
        HashSet<char> lettersPresent;
        HashSet<char> lettersMissing;
        List<HashSet<char>> yellowLetters;

        public GuessesRepository()
        {
            Reset();
        }

        public void Reset()
        {
            dictionary = Words.Dictionary.words.ToList();
            answers = Words.Answers.words.Distinct().ToList();
            bestGuesses = new List<Tuple<string, int>>();
            tree = new Trie();

            // Computed values
            foreach (var guess in new string[] { "soria", "serao", "sorai", "serio", "sorea", "oiras", "rosia", "risao", "seroa", "roias" })
                bestGuesses.Add(new Tuple<string, int>(guess, 0));

            lettersCorrect = new List<char>("_____");
            lettersPresent = new HashSet<char>();
            lettersMissing = new HashSet<char>();

            yellowLetters = new List<HashSet<char>>();

            for (int i = 0; i < 5; i++)
            {
                yellowLetters.Add(new HashSet<char>());
            }
        }

        public void ValidateAnswers()
        {
            List<string> validAnswers = new List<string>();

            foreach (var answer in answers)
            {
                bool valid = true;

                for (int i = 0; i < answer.Length; i++)
                {
                    if (lettersCorrect[i] != '_' && lettersCorrect[i] != answer[i])
                    {
                        valid = false;
                        break;
                    }

                    if (lettersMissing.Contains(answer[i]))
                    {
                        valid = false;
                        break;
                    }

                    if (yellowLetters[i].Contains(answer[i]))
                    {
                        valid = false;
                        break;
                    }

                    foreach (var letter in lettersPresent)
                    {
                        if (!answer.Contains(letter))
                        {
                            valid = false;
                            break;
                        }
                    }
                }

                if (valid)
                    validAnswers.Add(answer);
            }

            answers = validAnswers;
            tree = new Trie(answers);
        }

        public void InputGuess(string guess, List<GuessOutcome> outcome)
        {
            for(int i = 0; i < 5; i++)
            {
                if (outcome[i] == GuessOutcome.Correct)
                {
                    lettersCorrect[i] = guess[i];
                    lettersPresent.Add(guess[i]);
                }
                else if(outcome[i] == GuessOutcome.Present)
                {
                    lettersPresent.Add(guess[i]);
                    yellowLetters[i].Add(guess[i]);
                }
                else if(!lettersPresent.Contains(guess[i]))
                {
                    lettersMissing.Add(guess[i]);
                }
            }

            ValidateAnswers();

            ComputeGuesses();
        }

        public void ComputeGuesses()
        {
            bestGuesses.Clear();

#if Parallel
            object mutex = new object();

            dictionary.AsParallel().ForAll(word =>
#else
            SortedSet<int> bestScores = new SortedSet<int>();

            foreach(var word in answers)
#endif
            {
                int totalPossibilities = 0;

                foreach (var answer in answers)
                {
                    var correct = new List<char?>(new char?[] { null, null, null, null, null });
                    var present = 0;
                    var notPresent = 0;

                    ComputeOutcome(word, answer, correct, ref present, ref notPresent);

                    if (correct.Count(p => p == null) != 0)
                        totalPossibilities += tree.PossibleWords(correct, present, notPresent);

                    if (bestScores.Count >= 10 && totalPossibilities > bestScores.Last())
                        break;
                };

                if (bestScores.Count < 10 || totalPossibilities < bestScores.Last())
                {
                    bestScores.Add(totalPossibilities);
                    bestScores.Remove(bestScores.Last());
                }
#if Parallel
                lock (mutex)
                {
                    bestGuesses.Add(Tuple.Create(word, totalPossibilities));
                }
#else
                bestGuesses.Add(Tuple.Create(word, totalPossibilities));
#endif
            }

#if Parallel
            );
#endif 

            bestGuesses = bestGuesses.OrderBy(p => p.Item2).ToList();
        }

        internal void ComputeOutcome(string text, string answer, List<char?> correct, ref int present, ref int notPresent)
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == answer[i])
                {
                    correct[i] = text[i];

                    present |= 1 << (text[i] - 'a');
                }
                else if (answer.Contains(text[i]))
                {
                    present |= 1 << (text[i] - 'a');
                }
                else
                {
                    notPresent |= 1 << (text[i] - 'a');
                }
            }
        }

        public IEnumerable<string> BestGuesses
        {
            get 
            { 
                return bestGuesses.Take(10).Select(p => p.Item1);
            }
        }
    }
}
