﻿using System;
 using System.Collections.Generic;
namespace CnGalWebSite.DataModel.ViewModel.Articles
{
    public class EditArticleCanCommentModel
    {
        public long[] Ids { get; set; }

        public bool CanComment { get; set; }
    }
}
