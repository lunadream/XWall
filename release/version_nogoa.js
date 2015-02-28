var fso = new ActiveXObject("Scripting.FileSystemObject");

var version = fso.getFileVersion("x-wall-setup.exe");

var file = fso.openTextFile("version");
var info = file.readAll();
file.close();

var versions = info.split("\r\n");
versions[0] = version;

file = fso.createTextFile("version");
file.write(versions.join("\r\n"));
file.close();

//file = fso.createTextFile("..\\..\\gh-pages\\release\\version");
//file.write(version);
//file.close();