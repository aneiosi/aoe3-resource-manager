﻿namespace Resource_Manager.Classes.Bar
{
	public class BarEntryLastWriteTime
	{
		public BarEntryLastWriteTime(BinaryReader binaryReader)
		{
			Year = binaryReader.ReadInt16();
			Month = binaryReader.ReadInt16();
			DayOfWeek = binaryReader.ReadInt16();
			Day = binaryReader.ReadInt16();
			Hour = binaryReader.ReadInt16();
			Minute = binaryReader.ReadInt16();
			Second = binaryReader.ReadInt16();
			Msecond = binaryReader.ReadInt16();
		}

		public BarEntryLastWriteTime(DateTime dateTime)
		{
			Year = (short)dateTime.Year;
			Month = (short)dateTime.Month;
			DayOfWeek = (short)dateTime.DayOfWeek;
			Day = (short)dateTime.Day;
			Hour = (short)dateTime.Hour;
			Minute = (short)dateTime.Minute;
			Second = (short)dateTime.Second;
			Msecond = (short)dateTime.Millisecond;
		}

		public short Hour { get; }
		public short Minute { get; }
		public short Second { get; }
		public short Msecond { get; }
		public short Year { get; }
		public short Month { get; }
		public short Day { get; }
		public short DayOfWeek { get; }

		public byte[] ToByteArray()
		{
			using var ms = new MemoryStream();
			using var bw = new BinaryWriter(ms);
			bw.Write(Year);
			bw.Write(Month);
			bw.Write(DayOfWeek);
			bw.Write(Day);
			bw.Write(Hour);
			bw.Write(Minute);
			bw.Write(Second);
			bw.Write(Msecond);
			return ms.ToArray();
		}
	}
}
