(function () {
    var shell = new ActiveXObject("WScript.Shell");
    var result = shell.run('"C:\\Program Files (x86)\\Inno Setup 5\\ISCC.exe" "C:\\Projects\\X-Wall\\X-Wall\\release\\iss\\x-wall.iss"', 1, true);
    result = result || shell.run('"C:\\Program Files (x86)\\Inno Setup 5\\ISCC.exe" "C:\\Projects\\X-Wall\\X-Wall\\release\\iss\\x-wall-full.iss"', 1, true);

    if (result != 0) {
        WScript.echo("Build installer failed.");
        return;
    }

    var fso = new ActiveXObject("Scripting.FileSystemObject");
    try {
        fso.deleteFile("x-wall-setup-with-plonk.exe");
    }
    catch (e) { }

    shell.run("cmd /c mklink /h x-wall-setup-with-plonk.exe x-wall-setup.exe");

    var version = fso.getFileVersion("x-wall-setup.exe");

    var file = fso.openTextFile("version");
    var info = file.readAll();
    file.close();

    var versions = info.split("\r\n");
    versions[0] = version;

    file = fso.createTextFile("version");
    file.write(versions.join("\r\n"));
    file.close();

    file = fso.createTextFile("..\\..\\gh-pages\\release\\version");
    file.write(version);
    file.close();
})();