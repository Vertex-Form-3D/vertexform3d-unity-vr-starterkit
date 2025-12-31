var LibraryTimerWorker = {
    Photon_JS_TimerWorker_Start: function(callback, interval) {
        console.log("[Photon] Photon_JS_TimerWorker_Start", interval);
        
        if (!Module.PhotonVoice_TimerWorker_Global) {
            Module.PhotonVoice_TimerWorker_Global = {
                HandleCnt: 0,
                Handles: new Map(),
            };
        }
        
        const workerFoo = // minification-friendly "to string conversion", comment out `s for dev
        `
        function() {
            let timer;
            const job = (ev) => {
                const interval = ev.data;
                timer = setInterval(() => postMessage(0), interval);
            }
            onmessage = job;
        }
        `

        let ws = workerFoo.toString();
        ws = ws.substring(ws.indexOf("{") + 1, ws.lastIndexOf("}"));
        const blob = new Blob([ws], {
            type: "text/javascript"
        });

        const workerURL = window.URL.createObjectURL(blob);
        const worker = new Worker(workerURL);
        const handle = Module.PhotonVoice_TimerWorker_Global.HandleCnt++;
        worker.onmessage = () => { {{{ makeDynCall('vi', 'callback') }}}(handle); }
        worker.postMessage(interval);
        Module.PhotonVoice_TimerWorker_Global.Handles[handle] = worker;
        return handle;
    },
    
    Photon_JS_TimerWorker_Stop: function(handle) {
        console.log("[Photon] Photon_JS_TimerWorker_Stop", handle);
        Module.PhotonVoice_TimerWorker_Global.Handles[handle].terminate();
        Module.PhotonVoice_TimerWorker_Global.Handles.delete(handle);
        return 0
    }
};

mergeInto(LibraryManager.library, LibraryTimerWorker);
