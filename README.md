# WADH
A small download helper for World of Warcraft addons

![WADH](screenshot.png)

### What it is

- It´s a very simple and tiny .NET 6 application named WADH (**W**orld of Warcraft **A**ddon **D**ownload **H**elper). WADH is spoken like "wad-age", since my previous download manager (see [section](#why-it-exists) below) was written 'ages' ago... :stuck_out_tongue_winking_eye:
- WADH automatically downloads a bunch of specific zip files from https://www.curseforge.com into a specific folder. The folder and the zip file urls are specified in a XML config file.
- It´s just a typical ".exe file Windows application". Just download the newest release, unzip and run it. That´s it. There is no installer, setup or something like that.
- It´s sole purpose is to download a bunch of zip files (World of Warcraft addons, hosted at curseforge.com) into a folder. Nothing else. It´s just there to make your life a little bit easier.

### How it works

- WADH is using the Microsoft Edge WebView2 webview component, to open the addon zip file download sites (hosted at curseforge.com).
- Since curseforge.com is strictly protected by Cloudflare, a technique like this is at the moment the only way to fetch the addons.
- WADH is looking inside the `C:\Users\YOUR_USER_NAME\AppData\Local\MBODM` folder for a config file named `WADH.xml` (see file format below).
- WADH is loading that config file and download all given urls sequentially into the given folder, when the "Start" button was pressed.

### XML config file format

```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
	<folder>C:\Data\Addons</folder>
	<addons>
		<url>https://www.curseforge.com/wow/addons/coordinates</url>
		<url>https://www.curseforge.com/wow/addons/deadly-boss-mods</url>
		<url>https://www.curseforge.com/wow/addons/details</url>
		<url>https://www.curseforge.com/wow/addons/groupfinderflags</url>
		<url>https://www.curseforge.com/wow/addons/mythic-dungeon-tools</url>
		<url>https://www.curseforge.com/wow/addons/raiderio</url>
		<url>https://www.curseforge.com/wow/addons/simulationcraft</url>
		<url>https://www.curseforge.com/wow/addons/tomtom</url>
		<url>https://www.curseforge.com/wow/addons/weakauras-2</url>
	</addons>
</root>
```

The application validates if
- the file has a general XML file format
- the `root` tag exists
- the `folder` tag exists
- the `addons` tag exists
- the `folder` tag contains a valid folder path
- the `addons` tag contains at least 1 `url` tag
- all `url` tags (inside the `addons` tag) contain a valid Curse addon page url

All other tags or comments are simply ignored by the application.

### Why it exists
I developed a download manager for World of Warcraft addons, called [WADM](https://github.com/mbodm/wadm), over a decade ago. For many many years WADM handled all of our needs with ease, when it comes down to downloading and updating the addons. But since Curse/Overwolf changed their political stance, alternative download managers (like mine, Ajour, WowUp, or others) no longer works with the https://www.curseforge.com site, or their REST web service. The only option is to use their own addon download manager. Many of us don´t want that, for different reasons.

For more information about "the end of all alternative addon download managers" follow the links on the GitHub site of my above mentioned WADM project, or use your GoogleFu.

So, for a while i was ok with downloading the addons manually (which is not the time consuming bottleneck here) and unzipping them (this is the time consuming part). Therefore i wrote a tool named [WAUZ](https://github.com/mbodm/wauz) to make the unzip process way easier. I downloaded the addons with the help of my browser and some direct-download-link bookmarks. This was good enough and made addon updating less painful, but i still tried some better solutions here and there.

Awesonium (or other ways to embed a full fledged web browser) did not the trick for me. And web scraping tools like Scrappy, Axios, or Puppeteer were also not my type of deal. These days i played a bit with the Microsoft Edge web engine component (named WebView2), just for fun. And i liked it. The result, as some first shot, is this small tool. Combined with above mentioned WAUZ tool it makes my live even more easy. 😁

### Requirements

- 64-bit Windows

There are not any other special requirements. All the release-binaries are compiled with _win-x64_ as target platform, assuming you are using some 64-bit Windows (and that's quite likely).

You can choose between _self-contained_ and _framework-dependent_ .NET application builds, when downloading a release. If you want to run the _framework-dependent_ version, you need (as additional requirement) the .NET 6 runtime installed on your machine. You can find more information about that topic on the [Releases](https://github.com/mbodm/wadh/releases) page.

### Notes
- WADH loads your selected downlod folder and the addon urls from a config file.
- WADH loads that data when you press the "Start" button.
- WADH writes a log file if some error happens.
- You can find both files (config and log) in the "C:\Users\YOUR_USER_NAME\AppData\Local\MBODM" folder.
- WADH is using the Microsoft Edge WebView2 webview component, to access the download site.
- The reason for this: https://www.curseforge.com is protected by Cloudflare.
- WADH deletes all zip files in the given download folder, before the download starts.
- WADH is written in C# and developed with .NET 6, in Visual Studio 2022.
- WADH is using Windows.Forms as UI framework (yes, because "rapid development").
- I never compiled WADH with other tools, like Rider or VS Code. I solely used Visual Studio 2022 Community.
- If you want to compile by yourself, you can just use i.e. Visual Studio 2022 (any edition). You need nothing else.
- The release-binaries are compiled with "win-x64" as target platform (self-contained and framework-dependent).
- The release-binaries are compiled with "ReadyToRun compilation", checked in the Visual Studio Publish dialog.
- WADH is under MIT license. Feel free to use the source and do whatever you want. I assume no liability.
- WADH just exists, because i am lazy and made my life a bit easier, by writing this tool (for me and some friends). 

#### Have fun.
