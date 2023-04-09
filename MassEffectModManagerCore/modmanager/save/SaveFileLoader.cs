﻿using System.IO;
using LegendaryExplorerCore.Coalesced;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using ME3TweaksModManager.modmanager.save.game2.FileFormats;
using ME3TweaksModManager.modmanager.save.game2;
using ME3TweaksModManager.modmanager.save.game3;

namespace ME3TweaksModManager.modmanager.save
{
    public class SaveFileLoader
    {
        public static ISaveFile LoadSaveFile(Stream stream, MEGame game, string fileName = null)
        {
            var version = stream.ReadInt32();
            stream.Position -= 4;
            switch (version)
            {
                // Todo: LE1
                case 29:
                    // This is ME2... wonder how it's different...
                    return Read(stream, fileName, MEGame.ME2);
                case 30:
                    return Read(stream, fileName, MEGame.LE2);
                case 59: // ME3/LE3 share the same format
                    return Read(stream, fileName, MEGame.LE3);
                default:
                    throw new Exception($@"Save version not supported: {version}");

            }

            return null;
        }

        public static ISaveFile Read(Stream input, string fileName = null, MEGame expectedGame = MEGame.Unknown)
        {

            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            ISaveFile save = null;
            switch (expectedGame)
            {
                case MEGame.LE2:
                    save = new LE2SaveFile();
                    break;
                case MEGame.ME3:
                case MEGame.LE3:
                    save = new SaveFileGame3();
                    break;
                default:
                    throw new Exception(@"Loader not implemented");
            }

            save.SaveFilePath = fileName;

            if (fileName != null)
            {
                // Setup save params
                var sgName = Path.GetFileNameWithoutExtension(fileName);
                if (sgName.StartsWith("Save_"))
                {
                    // Parse number
                    var numStr = sgName.Substring(sgName.IndexOf("_") + 1);
                    if (int.TryParse(numStr, out var saveNum))
                    {
                        save.SaveNumber = saveNum;
                        save.SaveGameType = ESFXSaveGameType.SaveGameType_Manual;
                    }
                }
                else if (sgName.StartsWith("AutoSave"))
                {
                    save.SaveGameType = ESFXSaveGameType.SaveGameType_Auto;
                }
                else if (sgName.StartsWith("ChapterSave"))
                {
                    save.SaveGameType = ESFXSaveGameType.SaveGameType_Chapter;
                }
                else if (sgName.StartsWith("QuickSave"))
                {
                    save.SaveGameType = ESFXSaveGameType.SaveGameType_Quick;
                }
            }


            var reader = new UnrealStream(input, true, 30);
            save.Serialize(reader);
            var crc = input.ReadUInt32();
            if (input.Position != input.Length)
            {
                save.IsValid = false;
#if DEBUG
                throw new FormatException("did not consume entire file");
#endif
                return save;
            }
            input.Position = 0;

            var calculatedCRC = Crc32.Compute(input.ReadToBuffer(input.Length - 4));
            if (crc != calculatedCRC)
            {
                save.IsValid = false;
            }
            return save;
        }
    }
}
