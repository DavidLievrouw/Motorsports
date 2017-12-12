//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Motorsports.Scaffolding.Web.App_Code
{
    using System;
    using System.Collections.Generic;
    
    public partial class Season
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Season()
        {
            this.Rounds = new HashSet<Round>();
            this.SeasonWinners = new HashSet<SeasonWinner>();
        }
    
        public int Id { get; set; }
        public string Sport { get; set; }
        public string Label { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Round> Rounds { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<SeasonWinner> SeasonWinners { get; set; }
        public virtual Sport RelatedSport { get; set; }
        public virtual Team Team { get; set; }
    }
}