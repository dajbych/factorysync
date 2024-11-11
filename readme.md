# FactorySync

Want to play Factorio with your friends? Do you want to play one map together without having to agree with each other when who will host the game, and is it too complicated for you to set up and maintain a separate server just for that?  Then this utility is just for you.

## Installation

⬇️ [**Download**](https://www.dropbox.com/scl/fi/0nygn0wjnubrfj43dg8ig/FactorySync.msi?rlkey=uts9a3u1jitdk0ttanfpdku7p&st=y87g8bnt&dl=1) ️(version **0.0.3**)

The utility runs on Windows 64-bit with .NET Framework 4.8, which is part of Windows 10 version 1903 and above. It is distributed in the form of an MSI installation package (Windows Installer).

## How to use it

FactorySync is a Windows app that runs on the background (it is optimized to consume as little CPU and RAM as possible). It synchronizes game save between the directory where Factorio stores them and the directory that syncs with online storage (such as OneDrive, Google Drive, iCloud, Dropbox, and others). When you share the directory in the online storage among your friends, each of them will use this utility and all of them follow simple principles, the game will be automatically synchronized between players’ computers.

## Usage principles

Synchronization may be more complicated than you think. Although this utility cannot perform miracles, it tries to do so. In order for players not to lose their progress in the game, it is important that they always follow the following procedure.

### 1️⃣ Always join to existing server

**Always join to the existing server whenever possible.** If you start playing a game in single player mode, other players in your game cannot play. This will create a fork and your work in this game will be wasted. (When this happens to you, you'll need to make blueprints of what you've done, transfer them to the original game, and build them there again.)

### 2️⃣ Always play multiplayer

**Only when none of your friends are hosting the game, you can start hosting the game yourself.** It allows others to join to the latest state of the game. In theory, this wouldn't be necessary if it was guaranteed that two players would never play at the same time. But you simply cannot rely on it (and especially you don't want to think about it, but instead focus on the mechanics of the game).

### 3️⃣ Always save the game when the server goes offline

**Always save the game when the server goes offline.** You can never be sure that it has the current state of the game saved and that it will reach you. If other players want to continue, agree among yourselves who will host the game. It must be just one player that the other players join. The utility can sync saves, but it can't (and no other tool ever will) merge two forked game positions back into one, like Git does with source files.

## How it works

Although any file synchronization tool could theoretically be used instead of this utility, it would not be a good idea. Relying on the fact that the date of the file will always be set correctly is not enough. This tool was created to compare in-game time.

To do this, the utility opens the game file and tries to read the game time from it. It's not as easy as it might seem because there are no libraries or documentation for this from the game developers. The game's file format also changes frequently. Therefore, it is necessary to keep this utility up to date.

The application is designed to overwrite the old file with the new one only if it is sure that the game time was read correctly. This is to make the game positions sync reliably. At the same time, this means that when the game save format changes, the new save will be uploaded to the online storage only after the application is updated. On the other hand, the app will notify you about it.

The utility also covers a specific case that is typical only for gaming. If the attack on the biters or the first flight with the space platform fails, you will be returned to the original game position (which can also be an autosave). In this case, the synchronized save may be newer than the current one. However, you don't want this newer file to overwrite your restored game position. This won't happen if the app is on while playing (because it only promotes changed files). However, in order for the restored game to sync to the directory, it is necessary to exceed the playing time of the game stored in it.
