var LibraryWebSockets = {
$webSocketInstances: [],

SocketCreate: function(url, protocols, openCallback, recvCallback, errorCallback, closeCallback)
{
    var str = UTF8ToString(url);
    var prot = UTF8ToString(protocols);
    var socket = {
        socket: new WebSocket(str, [prot]),
        error: null,
    }
    var instance = webSocketInstances.push(socket) - 1;
    socket.socket.binaryType = 'arraybuffer';
    
    socket.socket.onopen = function () {
    Module.dynCall_
        Module.dynCall_vi(openCallback, instance);
    }
    socket.socket.onmessage = function (e) {
        if (e.data instanceof ArrayBuffer)
        {
            const b = e.data;
            const ptr = _malloc(b.byteLength);
            const dataHeap = new Int8Array(HEAPU8.buffer, ptr, b.byteLength);
            dataHeap.set(new Int8Array(b));
            Module.dynCall_viii(recvCallback, instance, ptr, b.byteLength);
            _free(ptr);
        }
    };
    socket.socket.onerror = function (e) {
        Module.dynCall_vii(errorCallback, instance, e.code);
    }
    socket.socket.onclose = function (e) {
        if (e.code != 1000)
        {
            Module.dynCall_vii(closeCallback, instance, e.code);
        }
    }
    return instance;
},

SocketState: function (socketInstance)
{
    var socket = webSocketInstances[socketInstance];
    return socket.socket.readyState;
},

SocketError: function (socketInstance, ptr, bufsize)
{
 	var socket = webSocketInstances[socketInstance];
 	if (socket.error == null)
 		return 0;
    stringToUTF8(socket.error, ptr, bufsize);
    return 1;
},

SocketSend: function (socketInstance, ptr, length)
{
    var socket = webSocketInstances[socketInstance];
    socket.socket.send (HEAPU8.buffer.slice(ptr, ptr+length));
},

SocketClose: function (socketInstance)
{
    var socket = webSocketInstances[socketInstance];
    socket.socket.close();
}
};

autoAddDeps(LibraryWebSockets, '$webSocketInstances');
mergeInto(LibraryManager.library, LibraryWebSockets);
