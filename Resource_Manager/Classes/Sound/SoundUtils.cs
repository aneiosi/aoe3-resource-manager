using System;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;

namespace Resource_Manager.Classes.Sound
{
	public static class SoundUtils
	{
		public async static Task<byte[]> DecryptSound(byte[] source)
		{
			ulong qword8 = 0x23966BA95E28C33F;
			ulong qword10 = 0x39BAE3441DB35873;
			ulong qword18 = 0x2AF92545ADDE0B65;

			int non_padded_size = source.Length;
			int padding_length = (8 - non_padded_size % 8) % 8;

			byte[] data = new byte[non_padded_size + padding_length];
			long currentBlock = data.Length / 8;

			Array.Copy(source, data, source.Length);

			using (var streamReader = new MemoryStream(data, false))
			using (var reader = new BinaryReader(streamReader))
			using (var streamWriter = new MemoryStream())
			using (var writer = new BinaryWriter(streamWriter))
			{
				await Task.Run(() =>
				{
					while (currentBlock > 0)
					{
						var block = reader.ReadUInt64();
						qword18 = BitOperations.RotateLeft(qword10 * (qword18 + qword8), 32);
						writer.Write(block ^ qword18);
						currentBlock--;
					}
				});
				return streamWriter.ToArray();
			}
		}
	}
}
