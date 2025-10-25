using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MQGateway.Core.Entities
{
	[Table("data")]
	internal class Data
	{
		[Key]
		[Column("id_data")]
		public int ID_Data { get; set; }

		[Required]
		[Column("id_topic")]
		public int ID_Topic { get; set; }

		[Required]
		[Column("value_data")]
		public string? Value_Data { get; set; }

		[Required]
		[Column("time_data")]
		public DateTime Time_Data { get; set; }

		//-------------------------------------------------------------

		public Topic? Topic { get; set; }
	}
}
