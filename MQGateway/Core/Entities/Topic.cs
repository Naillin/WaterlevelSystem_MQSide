using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MQGateway.Core.Entities
{
	[Table("topics")]
	internal class Topic
	{
		[Key]
		[Column("id_topic")]
		public int ID_Topic { get; set; }

		[MaxLength(255)]
		[MinLength(1)]
		[Required]
		[Column("name_topic")]
		public string? Name_Topic { get; set; }

		[MaxLength(255)]
		[MinLength(1)]
		[Required]
		[Column("path_topic")]
		public string? Path_Topic { get; set; }

		[Required]
		[Column("latitude_topic")]
		public double Latitude_Topic { get; set; }

		[Required]
		[Column("longitude_topic")]
		public double Longitude_Topic { get; set; }

		[Required]
		[Column("altitude_topic")]
		public double Altitude_Topic { get; set; }

		[Required]
		[Column("altitudesensor_topic")]
		public double AltitudeSensor_Topic { get; set; }

		//-------------------------------------------------------------

		public ICollection<Data> Data { get; set; }

		public ICollection<AreaPoint> AreaPoints { get; set; }

		public Topic()
		{
			Data = new List<Data>();
			AreaPoints = new List<AreaPoint>();
		}
	}
}
