using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TrashRush.EditorTools
{
    // Single-model VOX files, as exported by Goxel. No Unity/native dependencies.
    internal static class VoxBrickReader
    {
        public static Model Read(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var reader = new BinaryReader(stream, Encoding.ASCII))
            {
                ValidateFileHeader(reader);

                var model = new Model();
                bool hasSize = false;
                bool hasPalette = false;

                while (stream.Position < stream.Length)
                {
                    string chunkId = ReadChunkHeader(reader, out int contentSize, out long contentEnd);

                    switch (chunkId)
                    {
                        case "PACK":
                            if (contentSize != 4 || reader.ReadInt32() != 1)
                            {
                                throw new InvalidDataException("Multiple VOX models are not supported. Export a single model.");
                            }

                            break;

                        case "SIZE":
                            ReadDimensions(reader, model, contentSize, hasSize);
                            hasSize = true;
                            break;

                        case "XYZI":
                            ReadCells(reader, model, contentSize, hasSize);
                            break;

                        case "RGBA":
                            ReadPalette(reader, model, contentSize, hasPalette);
                            hasPalette = true;
                            break;

                        case "nTRN":
                        case "nGRP":
                        case "nSHP":
                            throw new InvalidDataException("VOX scene transforms are not supported. Export a single volume from Goxel.");
                    }

                    stream.Position = contentEnd;
                }

                if (model.Cells == null || !hasPalette)
                {
                    throw new InvalidDataException("The model or RGBA palette is missing. Export in MagicaVoxel format from Goxel.");
                }

                return model;
            }
        }

        private static void ValidateFileHeader(BinaryReader reader)
        {
            Stream stream = reader.BaseStream;

            if (stream.Length < 20 || Encoding.ASCII.GetString(reader.ReadBytes(4)) != "VOX ")
            {
                throw new InvalidDataException("The file is not a valid MagicaVoxel .vox file.");
            }

            int version = reader.ReadInt32();

            if (version != 150 && version != 200)
            {
                throw new InvalidDataException($"Unsupported VOX version: {version}.");
            }

            if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "MAIN")
            {
                throw new InvalidDataException("The VOX MAIN chunk is missing.");
            }

            int mainContentSize = reader.ReadInt32();
            int mainChildrenSize = reader.ReadInt32();

            if (mainContentSize != 0 || mainChildrenSize < 0 || 20L + mainChildrenSize != stream.Length)
            {
                throw new InvalidDataException("The VOX chunk lengths are invalid.");
            }
        }

        private static string ReadChunkHeader(BinaryReader reader, out int contentSize, out long contentEnd)
        {
            Stream stream = reader.BaseStream;

            if (stream.Length - stream.Position < 12)
            {
                throw new InvalidDataException("The VOX chunk header is incomplete.");
            }

            string chunkId = Encoding.ASCII.GetString(reader.ReadBytes(4));
            contentSize = reader.ReadInt32();
            int childrenSize = reader.ReadInt32();
            contentEnd = stream.Position + contentSize;

            if (contentSize < 0 || childrenSize < 0 || contentEnd + childrenSize > stream.Length)
            {
                throw new InvalidDataException("The VOX chunk is incomplete or corrupted.");
            }

            // Goxel's single-volume export has no nested chunks.
            if (childrenSize != 0)
            {
                throw new InvalidDataException("Nested chunks are not supported. Export a single model from Goxel.");
            }

            return chunkId;
        }

        private static void ReadDimensions(BinaryReader reader, Model model, int contentSize, bool hasSize)
        {
            if (hasSize || contentSize != 12)
            {
                throw new InvalidDataException("Exactly one SIZE chunk is required.");
            }

            model.Width = reader.ReadInt32();
            model.Depth = reader.ReadInt32();
            model.Height = reader.ReadInt32();

            if (!IsValidDimension(model.Width) || !IsValidDimension(model.Depth) || !IsValidDimension(model.Height))
            {
                throw new InvalidDataException("VOX dimensions must be between 1 and 256.");
            }
        }

        private static bool IsValidDimension(int dimension)
        {
            return dimension >= 1 && dimension <= 256;
        }

        private static void ReadCells(BinaryReader reader, Model model, int contentSize, bool hasSize)
        {
            if (!hasSize || model.Cells != null || contentSize < 4)
            {
                throw new InvalidDataException("A single SIZE chunk followed by a single XYZI chunk is required.");
            }

            int cellCount = reader.ReadInt32();

            if (cellCount < 1 || 4L + cellCount * 4L != contentSize || cellCount > model.Width * model.Depth * model.Height)
            {
                throw new InvalidDataException("The VOX cell count is invalid.");
            }

            model.Cells = new Cell[cellCount];
            var occupiedPositions = new HashSet<int>();

            for (int index = 0; index < cellCount; index++)
            {
                var cell = new Cell(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());

                if (cell.Color == 0 || cell.X >= model.Width || cell.Y >= model.Depth || cell.Z >= model.Height || !occupiedPositions.Add(cell.Key))
                {
                    throw new InvalidDataException("The VOX model contains an invalid or duplicate cell.");
                }

                model.Cells[index] = cell;
            }
        }

        private static void ReadPalette(BinaryReader reader, Model model, int contentSize, bool hasPalette)
        {
            if (contentSize != 1024 || hasPalette)
            {
                throw new InvalidDataException("The VOX color palette is invalid.");
            }

            for (int index = 1; index < 256; index++)
            {
                model.Palette[index] = reader.ReadBytes(4);
            }

            reader.ReadBytes(4); // Last entry is reserved, not color index 256.
        }

        internal readonly struct Cell
        {
            public readonly byte X;
            public readonly byte Y;
            public readonly byte Z;
            public readonly byte Color;

            public Cell(byte x, byte y, byte z, byte color)
            {
                X = x;
                Y = y;
                Z = z;
                Color = color;
            }

            public int Key => X | (Y << 8) | (Z << 16);
        }

        internal sealed class Model
        {
            public int Width;
            public int Depth;
            public int Height;
            public Cell[] Cells;

            // Palette index zero means empty. Index one is the first RGBA entry.
            public readonly byte[][] Palette = new byte[256][];
        }
    }
}
