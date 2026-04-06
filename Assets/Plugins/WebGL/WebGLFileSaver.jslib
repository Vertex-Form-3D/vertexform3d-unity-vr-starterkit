mergeInto(LibraryManager.library, {
    WebGLFileSaver_SaveFile: function(arrayPtr, size, fileNamePtr) {
        var bytes = new Uint8Array(size);
        for (var i = 0; i < size; i++) {
            bytes[i] = HEAPU8[arrayPtr + i];
        }
        var blob = new Blob([bytes], { type: 'application/octet-stream' });
        var fileName = UTF8ToString(fileNamePtr);

        var link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(link.href);
    }
});
