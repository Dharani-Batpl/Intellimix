
using System;
using System.Text.RegularExpressions; // For regex operations

namespace Intellimix_Core.Models
{

    
    public class MaterialTypeModel
    {

        // To read and write data to/from database

        // ? = nullable type
        public Guid material_type_id { get; set; }
        public string material_type_name { get; set; }
        public string material_type_code { get; set; }
        public string description { get; set; }
        public bool? hazardous_flag { get; set; }
        public string default_storage_condition { get; set; }
        public bool? active_flag { get; set; }
        public bool? deleted_flag { get; set; }
        public Guid? created_by { get; set; }
        public DateTime? created_at { get; set; }
        public Guid? updated_by { get; set; }
        public DateTime? updated_at { get; set; }
    }

    

}