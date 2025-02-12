﻿using System.Collections.Generic;
namespace CnGalWebSite.DataModel.ExamineModel
{
    public class EntryAddInfor
    {
        public List<BasicEntryInformation_> Information { get; set; } = new List<BasicEntryInformation_> { };
    }
    public class BasicEntryInformation_
    {
        public bool IsDelete { get; set; }
        /// <summary>
        /// 主索引
        /// </summary>
        public string DisplayName { get; set; }

        public string DisplayValue { get; set; }
        public string Modifier { get; set; }
        public List<BasicEntryInformationAdditional_> Additional { get; set; } = new List<BasicEntryInformationAdditional_>();
    }

    public class BasicEntryInformationAdditional_
    {
        public bool IsDelete { get; set; }
        /// <summary>
        /// 主索引
        /// </summary>
        public string DisplayName { get; set; }
        public string DisplayValue { get; set; }

    }
}
