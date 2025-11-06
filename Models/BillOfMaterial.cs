namespace IntelliMix_Core.Models
{
    public class BillOfMaterial
    {
        public string Program_Code { get; set; }
        public string Sequence_No { get; set; }
        public string _Created_By { get; set; }
        public string _Modified_By { get; set; }
        public DateTime? _Created_At { get; set; }
        public DateTime? _Modified_At { get; set; }
        public int NoOfSeq => Sequences?.Count ?? 0;

        public List<Sequence> Sequences { get; set; } = new List<Sequence>();




    }


    public class Sequence
    {
        public int? time { get; set; }
        public int? temp { get; set; }
        public int? rpm { get; set; }
        public int? energy { get; set; }
        public string mix_mode { get; set; }
        public string event1 { get; set; }
        public string event2 { get; set; }
        public string event3 { get; set; }
        public string event4 { get; set; }
        public string remarks { get; set; }
        public string sequence_no { get; set; }
    }


    public class KneaderEvents
    {
        public string event_code { get; set; }

        public string event_name { get; set; }

        public bool disable { get; set; }

    }



}
