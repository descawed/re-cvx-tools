﻿/*   ARCVX
 * 
 *   Copyright 2024 Kuriimu2 contributers (https://github.com/FanTranslatorsInternational/Kuriimu2)
 *   Copyright 2024 Kapdap <kapdap@pm.me> (https://github.com/kapdap)
 * 
 *   This program is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using ARCVX.Formats;
using Microsoft.Toolkit.HighPerformance;
using System;
using System.IO;

namespace ARCVX.Hash
{
    public static class Helper
    {
        public static MemoryStream WriteVerification(MemoryStream stream)
        {
            MemoryStream ms = new();

            byte[] block = new byte[HFS.BLOCK_SIZE];

            stream.Seek(0, SeekOrigin.Begin);

            for (int i = 0; i < stream.Length; i += HFS.BLOCK_SIZE)
            {
                int length = (int)Math.Min(HFS.BLOCK_SIZE, stream.Length - i);

                stream.Read(block, 0, length);

                ReadOnlySpan<byte> hash = new HfsHash().Compute(block.AsSpan(0, length));

                ms.Write(block, 0, length);
                ms.Write(hash);
            }

            ms.Seek(0, SeekOrigin.Begin);

            return ms;
        }

        public static ReadOnlySpan<byte> ComputeVerification(Span<byte> buffer) =>
            new HfsHash().Compute(buffer);
    }
}