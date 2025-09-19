using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MQGateway.Core.Entities
{
	[Table("area_points")]
	internal class AreaPoint
	{
		[Key]
		[Column("id_areapoint")]
		public int ID_AreaPoint { get; set; }

		[Required]
		[Column("id_topic")]
		public int ID_Topic { get; set; }

		[Column("depression_areapoint")]
		public string? Depression_AreaPoint { get; set; }

		//-------------------------------------------------------------

		public Topic? Topic { get; set; }
	}
}
