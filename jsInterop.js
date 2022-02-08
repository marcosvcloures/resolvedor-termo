startListeningKeyDown = (dotNetHelper) => {
    window.onkeydown = (evt) => {
        dotNetHelper.invokeMethodAsync('OnKeyDown', evt.key);
    };
}