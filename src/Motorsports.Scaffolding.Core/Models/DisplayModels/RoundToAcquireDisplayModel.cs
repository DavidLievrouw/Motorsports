using System;
using System.ComponentModel.DataAnnotations;

namespace Motorsports.Scaffolding.Core.Models.DisplayModels {
  public class RoundToAcquireDisplayModel {
    public RoundToAcquireDisplayModel(RoundToAcquire roundToAcquire) {
      Id = roundToAcquire.Id;
      Sport = roundToAcquire.Sport;
      Number = roundToAcquire.Number;
      Date = roundToAcquire.Date;
      Name = roundToAcquire.Name;
      Venue = roundToAcquire.Venue;
    }
    
    public int Id { get; set; }
    public string Sport { get; set; }
    public short Number { get; set; }
    public string Venue { get; set; }
    
    [DisplayFormat(NullDisplayText = "/")]
    public string Name { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}")]
    public DateTime Date { get; set; }
  }
}