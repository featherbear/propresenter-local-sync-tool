# ProPresenter 6 Local Sync Helper Tool
---

So like... half the time ProPresenter 6's Local Sync function (on Windows at least) is really buggy and doesn't play that nicely...  
So solution? Make my own!

## Usage
This tool can be run without any arguments, and will use the same synchronisation settings that ProPresenter 6 would use.
```
ppsync.exe [options]

= Sync Direction = 
  -d, --down           Download files to the repository
  -b, --both           Synchronise files to and from the repository
  -u, --up             Upload files to the repository
  
= Sync Media =
  -m, --media          Sync media
  -M, --no-media       Do not sync media
  
= Sync Library =
  -l, --library        Sync library
  -L, --no-library     Do not sync library
  
= Sync Templates =
  -t, --template       Sync templates
  -T, --no-template    Do not sync templates
  
= Sync Playlist =
  -p, --playlist       Sync playlists
  -P, --no-playlist    Do not sync playlists
  
= Replace existing items =
  -r, --replace        Replace files
  -R, --no-replace     Do not replace files
  
= Miscellanous =
  -s, --source         ProPresenter local sync source
  -q, --quiet          Suppresses output messages
  -h, --help           Display the help screen.
```

## Development

### Stuff used to make this:

 * [C#.NET](https://docs.microsoft.com/en-us/dotnet/csharp/csharp) `Built on .NET 4.5.2`
 * [Command Line Parser Library](https://github.com/gsscoder/commandline) `version 1.9.71`
 * [ILRepack](https://github.com/gluck/il-repack) `version 2.0.13`

### Building
* Use [NuGet](https://www.nuget.org/) to install [CommandLineParser](https://www.nuget.org/packages/CommandLineParser/) and [ILRepack](https://www.nuget.org/packages/ILRepack/) into the project

### Current Bugs / Issues
* Only supports _Default_ library

## License
Copyright © 2017 Andrew Wong  
ProPresenter is a registered trademark of Renewed Vision LLC.  
All rights reserved.  

This software is licensed under the GNU General Public License v3.0.  
You are free to redistribute it and/or modify it under the terms of the license.  
*For more details see the [LICENSE](https://raw.githubusercontent.com/bearbear12345/propresenter-local-sync-tool/master/LICENSE) file*
