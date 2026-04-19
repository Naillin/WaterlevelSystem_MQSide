using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MQGateway.Core.Entities;

[Table("emas")]
internal class Ema
{
    [Key]
    [Column("id_ema")]
    public int ID_Ema { get; set; }
    
    [Required]
    [Column("id_topic")]
    public int ID_Topic { get; set; }
    
    [Column("value_ema")]
    [JsonPropertyName("Value_Ema")]
    public string? Value_Ema { get; set; }

    //-------------------------------------------------------------

    public Topic? Topic { get; set; }
}