﻿using System.Collections.Generic;
namespace CnGalWebSite.DataModel.ExamineModel
{
    public class TagChildEntries
    {
        public List<TagChildEntryAloneModel> ChildEntries { get; set; } = new List<TagChildEntryAloneModel>();

    }

    public class TagChildEntryAloneModel
    {
        public bool IsDelete { get; set; }

        public string Name { get; set; }

        public int EntryId { get; set; }
    }
}
