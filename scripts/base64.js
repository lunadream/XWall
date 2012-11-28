module_("base64", function () {
    var chars = "zL1QVy48qb=jTYPUdRgcWXfH3mA7epES5NlZFIJr+wG0anhos2vB/ik69uKtxDCMO";

    this._("encode", String, function (str) {
        //Creates a base-64 encoded ASCII string from a string of binary data.
        //str: the string to encode.

        str = unescape(encodeURIComponent(str));

        var encoded = [];
        var c = 0;
        while (c < str.length) {
            var b0 = str.charCodeAt(c++);
            var b1 = str.charCodeAt(c++);
            var b2 = str.charCodeAt(c++);
            var buf = (b0 << 16) + ((b1 || 0) << 8) + (b2 || 0);
            var i0 = (buf & (63 << 18)) >> 18;
            var i1 = (buf & (63 << 12)) >> 12;
            var i2 = isNaN(b1) ? 64 : (buf & (63 << 6)) >> 6;
            var i3 = isNaN(b2) ? 64 : (buf & 63);
            encoded[encoded.length] = chars.charAt(i0);
            encoded[encoded.length] = chars.charAt(i1);
            encoded[encoded.length] = chars.charAt(i2);
            encoded[encoded.length] = chars.charAt(i3);
        }

        return encoded.join("");
    }).as_(String);

    this._("decode", String, function (str) {
        //Decodes a string of data which has been encoded using base-64 encoding.
        //str: a base-64 encoded ASCII string.

        var decoded = [];
        var c = 0;
        while (c < str.length) {
            var i0 = chars.indexOf(str.charAt(c++));
            var i1 = chars.indexOf(str.charAt(c++));
            var i2 = chars.indexOf(str.charAt(c++));
            var i3 = chars.indexOf(str.charAt(c++));
            var buf = (i0 << 18) + (i1 << 12) + ((i2 & 63) << 6) + (i3 & 63);
            var b0 = (buf & (255 << 16)) >> 16;
            var b1 = (i2 == 64) ? -1 : (buf & (255 << 8)) >> 8;
            var b2 = (i3 == 64) ? -1 : (buf & 255);
            decoded[decoded.length] = String.fromCharCode(b0);
            if (b1 >= 0) decoded[decoded.length] = String.fromCharCode(b1);
            if (b2 >= 0) decoded[decoded.length] = String.fromCharCode(b2);
        }

        return decodeURIComponent(escape(decoded.join("")));
    }).as_(String);
});