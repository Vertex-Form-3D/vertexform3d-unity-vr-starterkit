mergeInto(LibraryManager.library, {
    WebGLBrowser_DisableContextMenu: function() {
        document.addEventListener('contextmenu', function(e) {
            e.preventDefault();
        }, false);
    },

    WebGLBrowser_RequestFullscreen: function() {
        var canvas = document.querySelector('#unity-canvas') || document.querySelector('canvas');
        if (canvas) {
            if (canvas.requestFullscreen) canvas.requestFullscreen();
            else if (canvas.mozRequestFullScreen) canvas.mozRequestFullScreen();
            else if (canvas.webkitRequestFullscreen) canvas.webkitRequestFullscreen();
            else if (canvas.msRequestFullscreen) canvas.msRequestFullscreen();
        }
    },

    WebGLBrowser_ExitFullscreen: function() {
        if (document.exitFullscreen) document.exitFullscreen();
        else if (document.mozCancelFullScreen) document.mozCancelFullScreen();
        else if (document.webkitExitFullscreen) document.webkitExitFullscreen();
        else if (document.msExitFullscreen) document.msExitFullscreen();
    },

    WebGLBrowser_IsFullscreen: function() {
        return (document.fullscreenElement || document.mozFullScreenElement ||
                document.webkitFullscreenElement || document.msFullscreenElement) ? 1 : 0;
    },

    WebGLBrowser_ResumeAudioContext: function() {
        if (typeof WEBAudio !== 'undefined' && WEBAudio.audioContext &&
            WEBAudio.audioContext.state === 'suspended') {
            WEBAudio.audioContext.resume();
        }
    },

    VF3D_CreateBlobUrlFromBuffer: function (bufferPtr, length, mimePtr) {
        var bytes = HEAPU8.subarray(bufferPtr, bufferPtr + length);
        var mime = UTF8ToString(mimePtr);
        var blob = new Blob([bytes], { type: mime });
        var url = URL.createObjectURL(blob);
        var urlLength = lengthBytesUTF8(url) + 1;
        var urlBuffer = _malloc(urlLength);
        stringToUTF8(url, urlBuffer, urlLength);
        return urlBuffer;
    },

    VF3D_RevokeBlobUrl: function (urlPtr) {
        URL.revokeObjectURL(UTF8ToString(urlPtr));
    }
});
