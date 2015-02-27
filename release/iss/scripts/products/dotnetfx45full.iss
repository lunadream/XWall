// requires Windows Vista SP2, Windows 7 SP1, Windows 8, Windows 8.1, Windows Server 2008 SP2, Windows Server 2008 R2 SP1, Windows Server 2012 and Windows Server 2012 R2
// requires Windows Installer 3.1
// WARNING: express setup (downloads and installs the components depending on your OS) if you want to deploy it on cd or network download the full bootsrapper on website below
// http://www.microsoft.com/downloads/details.aspx?FamilyID=ab99342f-5d1a-413d-8319-81da479ab0d7

[CustomMessages]
dotnetfx4full_title=.NET Framework 4.5.2
dotnetfx4full_size=66.8MB

[Code]
const
	dotnetfx4full_url = 'http://download.microsoft.com/download/E/2/1/E21644B5-2DF2-47C2-91BD-63C560427900/NDP452-KB2901907-x86-x64-AllOS-ENU.exe';

procedure dotnetfx4full();
begin
	if (netfxspversion(NetFx40Full, '') < 1) then
		AddProduct('dotnetfx4full' + GetArchitectureString() + '.exe',
			'/lang:enu /passive /norestart',
			CustomMessage('dotnetfx4full_title'),
			CustomMessage('dotnetfx4full_size'),
			dotnetfx4full_url,
			false, false);
end;