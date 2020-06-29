﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WebService.Services;

namespace WebService.Models
{
    public class Sospecha
    {

        [Key]
        public Nullable<long> id { get; set; }
        public Nullable<int> age { get; set; }
        public string gender { get; set; }
        public Nullable<DateTime> sample_at { get; set; }
        public Nullable<int> epidemiological_week { get; set; }
        public string origin { get; set; }
        public string status { get; set; }
        public string run_medic { get; set; }
        public string symptoms { get; set; }
        public Nullable<DateTime> symptoms_at { get; set; }
        public Nullable<DateTime> reception_at { get; set; }
        public Nullable<long> receptor_id { get; set; }
        public Nullable<DateTime> result_ifd_at { get; set; }
        public string result_ifd { get; set; }
        public string subtype { get; set; }
        public Nullable<DateTime> pscr_sars_cov_2_at { get; set; }
        public string pscr_sars_cov_2 { get; set; }
        public string sample_type { get; set; }
        public Nullable<long> validator_id { get; set; }
        public Nullable<DateTime> sent_isp_at { get; set; }
        public string external_laboratory { get; set; }
        public Nullable<int> paho_flu { get; set; }
        public Nullable<int> epivigila { get; set; }
        public bool gestation { get; set; }
        public Nullable<int> gestation_week { get; set; }
        public Nullable<bool> close_contact { get; set; }
        public Nullable<bool> functionary { get; set; }
        public Nullable<DateTime> notification_at { get; set; }
        public string notification_mechanism { get; set; }
        public Nullable<DateTime> discharged_at { get; set; }
        public string observation { get; set; }
        public Nullable<bool> discharge_test { get; set; }
        public Nullable<long> minsal_ws_id { get; set; }
        public Nullable<long> patient_id { get; set; }
        public Nullable<long> laboratory_id { get; set; }
        public Nullable<long> establishment_id { get; set; }
        public Nullable<long> user_id { get; set; }
        public Nullable<DateTime> updated_at { get; set; }
        public Nullable<DateTime> created_at { get; set; }
        public Nullable<DateTime> deleted_at { get; set; }
    }
}