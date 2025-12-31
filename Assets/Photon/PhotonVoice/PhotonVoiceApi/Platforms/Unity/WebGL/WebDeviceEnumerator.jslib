mergeInto(LibraryManager.library, {
    PhotonVoice_WebRTC_EnumerateDevices: function(requestId, kind, resultCallback) {
        const kindStr = UTF8ToString(kind);
        if (navigator.mediaDevices && navigator.mediaDevices.getUserMedia && navigator.mediaDevices.enumerateDevices) {
            navigator.mediaDevices.getUserMedia({ audio: kindStr.startsWith("audio"), video: kindStr.startsWith("video") }).then(function(stream) {
                navigator.mediaDevices.enumerateDevices().then(devices => {
                    console.info('[PV] PhotonVoice_WebRTC_EnumerateDevices all:', kindStr, devices);
                    
                    // [id1, lbl1, id2, lbl2,...]
                    const res = devices.filter(d => d.kind == kindStr).map(d => [d.deviceId, d.label]).flat();
                    
                    console.info('[PV] PhotonVoice_WebRTC_EnumerateDevices res:', res);

                    const ptr = _malloc(res.length * 4);
                    for (let i = 0; i < res.length; i++) {
                        const s = res[i];
                        const l = s.length * 4 + 1;
                        const sPtr = _malloc(l);
                        stringToUTF8(s, sPtr, l)
                        Module.HEAPU32[ptr / 4 + i] = sPtr;
                    }

                    {{{ makeDynCall('viiii', 'resultCallback') }}}(requestId, 0, ptr, res.length);

                    for (let i = 0; i < res.length; i++) {
                        _free(ptr[i]);
                    }
                    _free(ptr);
                })
                .catch(function(err) {
                    console.error('[PV] PhotonVoice_WebRTC_EnumerateDevices enumerateDevices error: ' + err);
                    {{{ makeDynCall('viiii', 'resultCallback') }}}(requestId, 1, 0, 0);
                });
            })
            .catch(function(err) {
                console.error('[PV] PhotonVoice_WebRTC_EnumerateDevices enumerateDevices permission error: ' + err);
                {{{ makeDynCall('viiii', 'resultCallback') }}}(requestId, 2, 0, 0);
            });
        } else {
            console.error('[PV] PhotonVoice_WebRTC_EnumerateDevices error: ' + 'enumerateDevices not supported on your browser!');
            {{{ makeDynCall('viiii', 'resultCallback') }}}(requestId, 3, 0, 0);
        }
    },
});