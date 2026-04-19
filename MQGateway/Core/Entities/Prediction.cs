using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MQGateway.Core.Entities;

[Table("predictions")]
internal class Prediction
{
    [Key]
    [Column("id_prediction")]
    public int ID_Prediction { get; set; }

    [Required]
    [Column("id_topic")]
    public int ID_Topic { get; set; }
    
    [Column("value_prediction")]
    [JsonPropertyName("Value_Prediction")]
    public string? Value_Prediction { get; set; }
    
    [Column("time_prediction")]
    [JsonPropertyName("Time_Prediction")]
    public DateTimeOffset Time_Prediction { get; set; }

    //-------------------------------------------------------------

    public Topic? Topic { get; set; }
}