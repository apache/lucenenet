/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Text;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Config
{
	/// <summary>
	/// This
	/// <see cref="Sharpen.Format">Sharpen.Format</see>
	/// parses
	/// <see cref="long">long</see>
	/// into date strings and vice-versa. It
	/// uses the given
	/// <see cref="Sharpen.DateFormat">Sharpen.DateFormat</see>
	/// to parse and format dates, but before, it
	/// converts
	/// <see cref="long">long</see>
	/// to
	/// <see cref="System.DateTime">System.DateTime</see>
	/// objects or vice-versa.
	/// </summary>
	[System.Serializable]
	public class NumberDateFormat : NumberFormat
	{
		private const long serialVersionUID = 964823936071308283L;

		private readonly DateFormat dateFormat;

		/// <summary>
		/// Constructs a
		/// <see cref="NumberDateFormat">NumberDateFormat</see>
		/// object using the given
		/// <see cref="Sharpen.DateFormat">Sharpen.DateFormat</see>
		/// .
		/// </summary>
		/// <param name="dateFormat">
		/// 
		/// <see cref="Sharpen.DateFormat">Sharpen.DateFormat</see>
		/// used to parse and format dates
		/// </param>
		public NumberDateFormat(DateFormat dateFormat)
		{
			this.dateFormat = dateFormat;
		}

		public override StringBuilder Format(double number, StringBuilder toAppendTo, FieldPosition
			 pos)
		{
			return dateFormat.Format(Sharpen.Extensions.CreateDate((long)number), toAppendTo, 
				pos);
		}

		public override StringBuilder Format(long number, StringBuilder toAppendTo, FieldPosition
			 pos)
		{
			return dateFormat.Format(Sharpen.Extensions.CreateDate(number), toAppendTo, pos);
		}

		public override Number Parse(string source, ParsePosition parsePosition)
		{
			DateTime date = dateFormat.Parse(source, parsePosition);
			return (date == null) ? null : date.GetTime();
		}

		public override StringBuilder Format(object number, StringBuilder toAppendTo, FieldPosition
			 pos)
		{
			return dateFormat.Format(number, toAppendTo, pos);
		}
	}
}
