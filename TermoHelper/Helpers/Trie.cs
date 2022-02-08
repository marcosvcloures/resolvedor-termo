#if Parallel
using System.Collections.Concurrent;
#endif

namespace TermoHelper
{
    class Trie
    {
        Node root = new Node(0);

#if Parallel
        ConcurrentDictionary<Tuple<string, int, int>, int> database = new ConcurrentDictionary<Tuple<string, int, int>, int>();
#else
        Dictionary<Tuple<string, int, int>, int> database = new Dictionary<Tuple<string, int, int>, int>();
#endif

        public Trie()
        {

        }

        public Trie(List<string> words)
        {
            foreach(var word in words)
            {
                AddWord(word);
            }
        }

        internal void AddWord(string word)
        {
            root.AddWord(word);
        }

        public int PossibleWords(List<char?> correct, int present, int notPresent)
        {
            string correctText = string.Join("", correct.Select(x => x ?? ' '));

            var key = Tuple.Create(correctText, present, notPresent);

            if (database.ContainsKey(key))
                return database[key];

            return database[key] = root.PossibleWords(correct, present, notPresent);
        }
    }

    internal class Node
    {
        Dictionary<char, Node> children;
        int position;

        public Node(int position)
        {
            this.position = position;
            children = new Dictionary<char, Node>();
        }

        public int PossibleWords(List<char?> correct, int present, int notPresent)
        {
            if (position == 5 && present == 0)
            {
                return 1;
            }

            if (position == 5)
                return 0;

            int ans = 0;

            if (correct[position].HasValue)
            {
                if (children.ContainsKey(correct[position].Value))
                {
                    int letter = (1 << (correct[position].Value - 'a'));

                    if ((present & letter) > 0)
                        ans = children[correct[position].Value].PossibleWords(correct, present ^ letter, notPresent);
                    else
                        ans = children[correct[position].Value].PossibleWords(correct, present, notPresent);

                    return ans;
                }

                return 0;
            }

            foreach (var child in children)
            {
                int letter = (1 << (child.Key - 'a'));

                if ((notPresent & letter) == 0)
                {
                    if ((present & letter) > 0)
                        ans += child.Value.PossibleWords(correct, present ^ letter, notPresent);
                    else
                        ans += child.Value.PossibleWords(correct, present, notPresent);
                }
            }

            return ans;
        }

        public void AddWord(string word)
        {
            if (string.IsNullOrEmpty(word))
                return;

            char firstLetter = word[0];

            if (!children.ContainsKey(firstLetter))
            {
                children[firstLetter] = new Node(position + 1);
            }

            children[firstLetter].AddWord(string.Join("", word.Skip(1)));
        }
    }
}
