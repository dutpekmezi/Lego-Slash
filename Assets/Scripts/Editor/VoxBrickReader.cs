using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TrashRush.EditorTools
{
    // Single-model VOX files, as exported by Goxel. No Unity/native dependencies.
    internal static class VoxBrickReader
    {
        internal readonly struct Cell
        {
            public readonly byte X, Y, Z, Color;
            public Cell(byte x, byte y, byte z, byte color)
            { X = x; Y = y; Z = z; Color = color; }
            public int Key => X | (Y << 8) | (Z << 16);
        }

        internal sealed class Model
        {
            public int Width, Depth, Height;
            public Cell[] Cells;
            // Palette index zero means empty. Index one is the first RGBA entry.
            public readonly byte[][] Palette = new byte[256][];
        }

        public static Model Read(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var reader = new BinaryReader(stream, Encoding.ASCII))
            {
                if (stream.Length < 20 || Encoding.ASCII.GetString(reader.ReadBytes(4)) != "VOX ")
                    throw new InvalidDataException("Geçerli bir MagicaVoxel .vox dosyası değil.");
                var version = reader.ReadInt32();
                if (version != 150 && version != 200)
                    throw new InvalidDataException($"Desteklenmeyen VOX sürümü: {version}.");
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "MAIN")
                    throw new InvalidDataException("VOX MAIN bölümü bulunamadı.");
                int mainBytes = reader.ReadInt32(), childBytes = reader.ReadInt32();
                if (mainBytes != 0 || childBytes < 0 || 20L + childBytes != stream.Length)
                    throw new InvalidDataException("VOX dosyasının bölüm uzunlukları geçersiz.");

                var model = new Model();
                bool hasSize = false, hasPalette = false;
                while (stream.Position < stream.Length)
                {
                    if (stream.Length - stream.Position < 12)
                        throw new InvalidDataException("Eksik VOX bölüm başlığı.");
                    string tag = Encoding.ASCII.GetString(reader.ReadBytes(4));
                    int bytes = reader.ReadInt32(), children = reader.ReadInt32();
                    long end = stream.Position + bytes;
                    if (bytes < 0 || children < 0 || end + children > stream.Length)
                        throw new InvalidDataException("Eksik veya bozuk VOX bölümü.");
                    // Goxel's single-volume export has no nested chunks.
                    if (children != 0)
                        throw new InvalidDataException("İç içe modeller desteklenmiyor. Goxel'den tek model dışa aktar.");
                    switch (tag)
                    {
                        case "PACK":
                            if (bytes != 4 || reader.ReadInt32() != 1)
                                throw new InvalidDataException("Birden fazla VOX modeli desteklenmiyor. Tek model dışa aktar.");
                            break;
                        case "SIZE":
                            if (hasSize || bytes != 12)
                                throw new InvalidDataException("Tek bir SIZE bölümü bekleniyor.");
                            model.Width = reader.ReadInt32();
                            model.Depth = reader.ReadInt32();
                            model.Height = reader.ReadInt32();
                            if (model.Width < 1 || model.Width > 256 || model.Depth < 1 || model.Depth > 256 || model.Height < 1 || model.Height > 256)
                                throw new InvalidDataException("VOX boyutları 1–256 aralığında olmalı.");
                            hasSize = true;
                            break;
                        case "XYZI":
                            if (!hasSize || model.Cells != null || bytes < 4)
                                throw new InvalidDataException("Tek model için SIZE ve XYZI bölümleri bekleniyor.");
                            int count = reader.ReadInt32();
                            if (count < 1 || 4L + count * 4L != bytes || count > model.Width * model.Depth * model.Height)
                                throw new InvalidDataException("VOX hücre sayısı geçersiz.");
                            model.Cells = new Cell[count];
                            var occupied = new HashSet<int>();
                            for (int i = 0; i < count; i++)
                            {
                                var cell = new Cell(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
                                if (cell.Color == 0 || cell.X >= model.Width || cell.Y >= model.Depth || cell.Z >= model.Height || !occupied.Add(cell.Key))
                                    throw new InvalidDataException("Geçersiz veya tekrar eden VOX hücresi.");
                                model.Cells[i] = cell;
                            }
                            break;
                        case "RGBA":
                            if (bytes != 1024 || hasPalette)
                                throw new InvalidDataException("VOX renk paleti geçersiz.");
                            for (int i = 1; i < 256; i++) model.Palette[i] = reader.ReadBytes(4);
                            reader.ReadBytes(4); // Last entry is reserved, not color index 256.
                            hasPalette = true;
                            break;
                        case "nTRN":
                        case "nGRP":
                        case "nSHP":
                            throw new InvalidDataException("VOX sahne dönüşümleri desteklenmiyor. Goxel'den tek hacim olarak dışa aktar.");
                    }
                    stream.Position = end;
                }
                if (model.Cells == null || !hasPalette)
                    throw new InvalidDataException("Model veya RGBA paleti eksik. Goxel'den Magica Voxel formatında dışa aktar.");
                return model;
            }
        }
    }
}
