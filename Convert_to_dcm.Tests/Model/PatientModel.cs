using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convert_to_dcm.Model
{
    public class PatientModel
    {
        public string PatientID { get; set; }
        public string PatientName { get; set; }
        public string PatientBirthDate { get; set; }
        public string PatientSex { get; set; }
        public string PatientAge { get; set; }
        public string PatientDoc { get; set; }

        // Default constructor to initialize non-nullable string properties
        public PatientModel()
        {
            PatientID = string.Empty;
            PatientName = string.Empty;
            PatientBirthDate = string.Empty;
            PatientSex = string.Empty;
            PatientAge = string.Empty;
            PatientDoc = string.Empty;
        }
    }
}
