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

    /* Idle quit: try to close the tab; browsers only allow window.close() on script-opened
       tabs, so if we are still alive shortly after, replace the page with an end screen.
       Unity itself is shut down by Application.Quit() right after this call. */
    VF3D_CloseWindowOrShowEndScreen: function (messagePtr) {
        var message = UTF8ToString(messagePtr);
        try { window.close(); } catch (e) { }
        setTimeout(function () {
            if (document.hidden) return; /* close worked or tab is backgrounded-away */
            try {
                document.body.innerHTML =
                    '<div style="position:fixed;inset:0;display:flex;align-items:center;justify-content:center;' +
                    'background:#101014;color:#eee;font-family:system-ui,-apple-system,sans-serif;z-index:99999;">' +
                    '<div style="max-width:420px;padding:28px 24px;text-align:center;background:#1a1a1f;' +
                    'border:1px solid rgba(255,255,255,0.12);border-radius:16px;box-shadow:0 12px 40px rgba(0,0,0,0.55);">' +
                    '<h2 style="margin:0 0 10px 0;font-size:1.3rem;">Session ended</h2>' +
                    '<p style="margin:0 0 20px 0;color:#aaa;font-size:0.95rem;line-height:1.45;">' + message + '</p>' +
                    '<button onclick="window.location.reload()" style="padding:12px 22px;font-size:1rem;font-weight:600;' +
                    'border:none;border-radius:10px;background:linear-gradient(160deg,#2b7fff,#1560d4);color:#fff;cursor:pointer;">' +
                    'Rejoin</button></div></div>';
            } catch (e) { }
        }, 300);
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
