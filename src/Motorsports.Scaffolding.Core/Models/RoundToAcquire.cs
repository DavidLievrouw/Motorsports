using System;

namespace Motorsports.Scaffolding.Core.Models {
  public class RoundToAcquire {
    public int Id { get; set; }
    public string Sport { get; set; }
    public short Number { get; set; }
    public DateTime Date { get; set; }
    public string Name { get; set; }
    public string Venue { get; set; }
  }
}