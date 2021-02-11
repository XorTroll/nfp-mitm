![Logo](emutool/PcIcon.png)

# emuiibo

> Virtual amiibo (amiibo emulation) system for Nintendo Switch

# Table of contents

1. [Usage](#usage)
2. [Controlling emuiibo](#controlling-emuiibo)
3. [Virtual amiibo creation](#virtual-amiibo-creation)
4. [For developers](#for-developers)
5. [Raw dumps and encryption](#raw-dumps-and-encryption)
6. [Amiibo format](#amiibo-format)
7. [License exemption](#license-exemption)
8. [Credits](#credits)

## Usage

Build or download the latest release of emuiibo and extract the contents of 'SdOut' directory (inside 'emuiibo-v*.zip') in the root of your SD card.

emuiibo comes bundled with a Tesla overlay to control it quite easily.

For more detailed information of how to use emuiibo, check [the usage wiki](Usage.md).

### SD layout

- Emuiibo's directory is `sd:/emuiibo`.

- Virtual amiibos go inside `sd:/emuiibo/amiibo`. For instance, an amiibo named `MyMario` would be `sd:/emuiibo/amiibo/MyMario/<amiibo content>`.

- However, categories are supported by placing amiibos inside sub-directories (only inside a directory, like 3DS menu categories inside categories are not supported) - for instance: `sd:/emuiibo/amiibo/SSBU/Yoshi` would be a `Yoshi` amiibo inside `SSBU` category.

- A virtual amiibo is detected by emuiibo based on two aspects: a `amiibo.json` and a `amiibo.flag` file must exist inside the virtual amiibo's folder mentioned above. If you would like to disable a virtual amiibo from being recognised by emuiibo, just remove the flag file, and create it again to enable it.

- Every time the console is booted, emuiibo saves all the miis inside the console to the SD card. Format is `sd:/emuiibo/miis/<index> - <name>/mii-charinfo.bin`.

## Controlling emuiibo

- **Emulation status (on/off)**: when emuiibo's emulation status is on, it means that any game trying to access/read amiibos will be intercepted by emuiibo. When it's off, it means that amiibo services will work normally, and nothing will be intercepted. This is basically a toggle to globally disable or enable amiibo emulation.

- **Active virtual amiibo**: it's the amiibo which will be sent to the games which try to scan amiibos, if emulation is on. Via tools such as the overlay, one can change the active virtual amiibo.

- **Virtual amiibo status (connected/disconnected)**: when the active virtual amiibo is connected, it means that the amiibo is always "placed", as if you were holding a real amiibo on the NFC point and never moving it - the game always detects it. When it is disconnected, it means that you "removed" it, as if you just removed the amiibo from the NFC point. Some games might ask you to remove the amiibo after saving data, so you must disconnect the virtual amiibo to "simulate" that removal. This is a new feature in v0.5, which fixed errors, since emuiibo tried to handle this automatically in previous versions, causing some games to fail.

All this aspects can be seen/controlled via the overlay.

## Virtual amiibo creation

Emuiibo no longer accepts raw BIN dumps to emulate amiibos. Instead, you can use `emutool` PC tool in order to generate virtual amiibos.

![Screenshot](emutool/Screenshot.png)

## For developers

emuiibo hosts a custom service, also named `emuiibo`, which can be used to control amiibo emulation by IPC commands.

The overlay's code can be a good example to see how to control emuiibo from IPC and C++.

## Raw dumps and encryption

When using raw dumps, emuiibo doesn't save everything they have onto the new format, since some bits are encrypted. This is, for instance, why 20-heart Wolf Link amiibo dumps won't work with emuiibo, since the amiibo's app-area, where the console/game saves amiibo savedata, is an encrypted field, and it contains the information BOTW needs in order to recognize the feature.

You can manually extract it, using [amiitool](https://github.com/socram8888/amiitool). You'll need to find the retail amiibo key to use it.

Running the following will decrypt the raw dump:

```cmd
amiitool -d -k <key-file-bin> -i <amiibo-raw-dump-bin> -o <out-decrypted-bin>
```

You'll have the decrypted app-area section at offset 0xDC on the decrypted dump, of length 0xD8. By saving that as `<game-access-id>.bin` inside the `/areas` folder of a virtual amiibo being used by emuiibo, you can actually import the dump's savedata, which would allow for such things to work. Check below for a list of per-game access IDs.

## Amiibo format

Amiibos are, as stated above, directories with an `amiibo.json` and an `amiibo.flag` file. The flag is mainly there in case people would like to disable an amiibo and then re-enable it later.

The JSON file contains all the aspects and data an amiibo needs to provide to games, except a few aspects (per-game savedata, protocol and tag type...)

This are the properties an amiibo has:

- Name: the amiibo's name (max. 40 characters)

- UUID: it's a unique identifier for the amiibo, composed of 10 bytes. If the "uuid" field is not present in the JSON, emuiibo will randomize the UUID everytime amiibo data is sent to a game. This has potential benefits in certain games, like in BOTW, where amiibos can only be used once per day, but with randomized UUIDs this can be bypassed, and one can get infinite rewards scanning this amiibo infinite times.

- Mii: every amiibo has a mii associated with it (it's "owner"). Internally, miis consist on a 88-byte structure known as "charinfo", so emuiibo stores this data in a file (typically `mii-charinfo.bin`). For new amiibos, emuiibo uses the console's services to generate a random mii, but for those who would like to use a mii from their console, emuiibo dumps in `miis` directory the console's miis, so it's just a matter of copying and pasting/replacing charinfo bin files. *NOTE*: emuiibo contains the charinfo file's name in the JSON (`mii_charinfo_file`), so if the file ever gets renamed, don't forget to rename it in the JSON too, or emuiibo will generate a random mii for the file name in the JSON.

- First and last write dates: these are (as if it wasn't obvious) the first and last time the amiibo was written/modified. When a virtual amiibo is created with emutool, the current date is assigned to both dates, and when the amiibo is modified in console, emuiibo updates the last write date.

- Write counter: this is a number which is increased everytime the amiibo is modified (and emuiibo does so, imitating Nintendo), but when the number reaches 65535, it is no longer increased (the number is technically a 16-bit number)

- Version: this value technically represents the version of Nintendo's amiibo library (NFP), so emuiibo just defaults it to 0.

### Areas

Areas (application areas, technically) are per-game amiibo savedata. Technically, real amiibos can only save data for a single game, but emuiibo allows as many games as you want (since savedata is stored as files). This savedata is quite small, and tends to be 216 bytes or smaller.

emuiibo saves this data inside bin files at `areas` directory inside the amiibo's directory, and the bin file's name is the game's area access ID in hex format.

An access ID is a unique ID/number each game has for amiibo savedata, used to check if the game actually has savedata in an amiibo. Here's a list of games and their access IDs:

### Per-game access IDs

- Super Smash Bros. Ultimate: 0x34F80200

- Splatoon 2: 0x10162B00

- Breath of the Wild: 0x1019C800

- Link's Awakening: 0x3B440400

**NOTE**: if anyone is willing to make savedata editors for this amiibo saves, I'm pretty sure it would be extremely helpful for many users.

## License exemption

- The Ryujinx project/team is exempt from GPLv2 licensing, and may make use of emuiibo's code licensing it under their current license.

## Credits

- Everyone who contributed to the original **nfp-mitm** project (forks): *Subv, ogniK, averne, spx01, SciresM*

- **libstratosphere** project and libraries

- **AmiiboAPI** web API, which is used by `emutool` to get a proper, full amiibo list, in order to generate virtual amiibos.

- [**3DBrew**](https://www.3dbrew.org/wiki/Amiibo) for their detailed documentation of amiibos, even though some aspects are different on the Switch.

- **LoOkYe** for writing emuiibo's wiki and helping with support.

- **AD2076** and **AmonRaNet** for helping with the tesla overlay.

- **AmonRaNet** for overlay translation support.

- **Thog** / **Ryujinx** devs for reversing mii services and various of its types.

- **Citra** devs for several amiibo formats used in 3DS systems.

- **Manlibear** for helping with improvements and development of `emutool`.

- All the testers and supporters from my Discord server who were essential for making this project progress and become what it is now :)
