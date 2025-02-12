﻿using CnGalWebSite.APIServer.Application.Entries;
using CnGalWebSite.APIServer.Application.Helper;
using CnGalWebSite.APIServer.Application.Tags;
using CnGalWebSite.APIServer.DataReositories;
using CnGalWebSite.APIServer.ExamineX;
using CnGalWebSite.DataModel.ExamineModel;
using CnGalWebSite.DataModel.Helper;
using CnGalWebSite.DataModel.Model;
using CnGalWebSite.DataModel.ViewModel;
using CnGalWebSite.DataModel.ViewModel.Entries;
using CnGalWebSite.DataModel.ViewModel.Tags;
using CnGalWebSite.Helper.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CnGalWebSite.APIServer.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/tags/[action]")]
    public class TagsAPIController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRepository<Tag, int> _tagRepository;
        private readonly IRepository<Entry, int> _entryRepository;
        private readonly IRepository<Examine, long> _examineRepository;
        private readonly IAppHelper _appHelper;
        private readonly ITagService _tagService;
        private readonly IEntryService _entryService;
        private readonly IExamineService _examineService;

        public TagsAPIController(UserManager<ApplicationUser> userManager, IAppHelper appHelper, IRepository<Tag, int> tagRepository, ITagService tagService,
            IRepository<Entry, int> entryRepository, IExamineService examineService, IRepository<Examine, long> examineRepository, IEntryService entryService)
        {
            _userManager = userManager;
            _tagRepository = tagRepository;
            _appHelper = appHelper;
            _tagService = tagService;
            _entryRepository = entryRepository;
            _examineService = examineService;
            _examineRepository = examineRepository;
            _entryService = entryService;
        }




        [AllowAnonymous]
        [HttpGet("{_id}")]
        public async Task<ActionResult<TagIndexViewModel>> GetTagAsync(string _id)
        {

            //获取当前用户ID
            var user = await _appHelper.GetAPICurrentUserAsync(HttpContext);
            //通过Id获取词条 
            Tag tag = null;
            try
            {
                var id = -1;
                id = int.Parse(_id);
                tag = await _tagRepository.GetAll().AsNoTracking()
                    .Include(s => s.InverseParentCodeNavigation)
                    .Include(s => s.ParentCodeNavigation)
                    .Include(s => s.Entries)
                    .FirstOrDefaultAsync(x => x.Id == id);
            }
            catch
            {
                tag = await _tagRepository.GetAll().AsNoTracking()
                    .Include(s => s.InverseParentCodeNavigation)
                    .Include(s => s.ParentCodeNavigation)
                    .Include(s => s.Entries)
                    .FirstOrDefaultAsync(x => x.Name == ToolHelper.Base64DecodeName(_id));
            }
            if (tag == null)
            {
                return NotFound();
            }
            //判断当前是否隐藏
            if (tag.IsHidden == true)
            {
                if (user == null || await _userManager.IsInRoleAsync(user, "Admin") != true)
                {
                    return NotFound();
                }

            }

            //读取审核信息
            List<Examine> examineQuery = null;
            if (user != null)
            {
                examineQuery = await _examineRepository.GetAll().AsNoTracking()
                               .Where(s => s.TagId == tag.Id && s.ApplicationUserId == user.Id && s.IsPassed == null
                               && (s.Operation == Operation.EditTagMain || s.Operation == Operation.EditTagChildTags || s.Operation == Operation.EditTagChildEntries))
                               .Select(s => new Examine
                               {
                                   Operation = s.Operation,
                                   Context = s.Context
                               })
                               .ToListAsync();
            }

            if (user != null)
            {
                var examine = examineQuery.Find(s => s.Operation == Operation.EditTagMain);
                if (examine != null)
                {
                    await _tagService.UpdateTagDataAsync(tag, examine);
                }
                examine = await _examineService.GetUserTagActiveExamineAsync(tag.Id, user.Id, Operation.EditTagChildTags);
                if (examine != null)
                {
                    await _tagService.UpdateTagDataAsync(tag, examine);
                }
                examine = await _examineService.GetUserTagActiveExamineAsync(tag.Id, user.Id, Operation.EditTagChildEntries);
                if (examine != null)
                {
                    await _tagService.UpdateTagDataAsync(tag, examine);
                }
            }

            var model = await _tagService.GetTagViewModel(tag);


            if (user != null)
            {
                var examine = examineQuery.Find(s => s.Operation == Operation.EditTagMain);
                if (examine != null)
                {
                    model.MainState = EditState.Preview;
                }
                examine = await _examineService.GetUserTagActiveExamineAsync(tag.Id, user.Id, Operation.EditTagChildTags);
                if (examine != null)
                {
                    model.ChildTagsState = EditState.Preview;
                }
                examine = await _examineService.GetUserTagActiveExamineAsync(tag.Id, user.Id, Operation.EditTagChildEntries);
                if (examine != null)
                {
                    model.ChildEntriesState = EditState.Preview;
                }
            }

            var examiningList = new List<Operation>();
            if (user != null)
            {
                examiningList = await _examineRepository.GetAll().Where(s => s.TagId == tag.Id && s.ApplicationUserId != user.Id && s.IsPassed == null).Select(s => s.Operation).ToListAsync();

            }

            //获取各部分状态
            if (user != null)
            {
                if (model.MainState != EditState.Preview)
                {
                    if (examiningList.Any(s => s == Operation.EditTagMain))
                    {
                        model.MainState = EditState.locked;
                    }
                    else
                    {
                        model.MainState = EditState.Normal;
                    }
                }
                if (model.ChildTagsState != EditState.Preview)
                {
                    if (examiningList.Any(s => s == Operation.EditTagChildTags))
                    {
                        model.ChildTagsState = EditState.locked;
                    }
                    else
                    {
                        model.ChildTagsState = EditState.Normal;
                    }
                }
                if (model.ChildEntriesState != EditState.Preview)
                {
                    if (examiningList.Any(s => s == Operation.EditTagChildEntries))
                    {
                        model.ChildEntriesState = EditState.locked;
                    }
                    else
                    {
                        model.ChildEntriesState = EditState.Normal;
                    }
                }
            }

            //增加阅读人数
            await _tagRepository.GetRangeUpdateTable().Where(s => s.Id == tag.Id).Set(s => s.ReaderCount, b => b.ReaderCount + 1).ExecuteAsync();

            return model;

        }

        /// <summary>
        /// 获取搜索结果 标签数量很少 可以简化
        /// </summary>
        /// <param name="tagName"></param>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<TagTreeModel>>> GetTagsTreeViewAsync()
        {

            var tags = await _tagRepository.GetAll().Where(s => s.ParentCodeNavigation == null && string.IsNullOrWhiteSpace(s.Name) == false && s.IsHidden == false).AsNoTracking()
                .Include(s => s.InverseParentCodeNavigation).ThenInclude(s => s.InverseParentCodeNavigation).ThenInclude(s => s.InverseParentCodeNavigation).ThenInclude(s => s.InverseParentCodeNavigation)
                .ToListAsync();

            var result = new List<TagTreeModel>();
            foreach (var item in tags)
            {
                var temp = new TagTreeModel
                {
                    Title = item.Name,
                    Id = item.Id,
                    Icon = item.Id switch
                    {
                        1 => "mdi-gamepad-square",
                        2 => "mdi-account ",
                        3 => "mdi-star",
                        4 => "mdi-group ",
                        _ => "mdi-tag-multiple-outline"
                    },
                    Children = item.InverseParentCodeNavigation.Where(s => string.IsNullOrWhiteSpace(s.Name) == false && s.IsHidden == false)
                    .Select(s => new TagTreeModel
                    {
                        Id = s.Id,
                        Title = s.Name,
                        Icon = s.InverseParentCodeNavigation.Any() ? "mdi-tag-multiple " : "mdi-tag-outline ",
                        Children = s.InverseParentCodeNavigation.Where(s => string.IsNullOrWhiteSpace(s.Name) == false && s.IsHidden == false)
                        .Select(s => new TagTreeModel
                        {
                            Id = s.Id,
                            Title = s.Name,
                            Icon = s.InverseParentCodeNavigation.Any() ? "mdi-tag-multiple " : "mdi-tag-outline ",

                            Children = s.InverseParentCodeNavigation.Where(s => string.IsNullOrWhiteSpace(s.Name) == false && s.IsHidden == false)
                            .Select(s => new TagTreeModel
                            {
                                Id = s.Id,
                                Title = s.Name,
                                Icon = s.InverseParentCodeNavigation.Any() ? "mdi-tag-multiple " : "mdi-tag-outline ",

                                Children = s.InverseParentCodeNavigation.Where(s => string.IsNullOrWhiteSpace(s.Name) == false && s.IsHidden == false)
                                .Select(s => new TagTreeModel
                                {
                                    Id = s.Id,
                                    Title = s.Name
                                }).ToList()
                            }).ToList()
                        }).ToList()
                    }).ToList()
                };

                result.Add(temp);
            }


            return result;

        }

        [HttpPost]
        public async Task<ActionResult<Result>> CreateTagAsync(CreateTagViewModel model)
        {
            try
            {
                //获取当前用户ID
                var user = await _appHelper.GetAPICurrentUserAsync(HttpContext);
                //检查是否超过编辑上限
                if (await _examineRepository.CountAsync(s => s.ApplicationUserId == user.Id && s.IsPassed == null) > ToolHelper.MaxEditorCount)
                {
                    return new Result { Successful = false, Error = "当前已超过最大待审核编辑数目，请等待审核通过后继续编辑，长时间未更新请联系管理员" };
                }

                //判断名称是否重复
                if (await _tagRepository.FirstOrDefaultAsync(s => s.Name == model.Main.Name) != null)
                {
                    return new Result { Error = "该标签的名称与其他标签重复", Successful = false };
                }
                //判断父标签是否存在
                Tag parentTag = null;

                if (string.IsNullOrWhiteSpace(model.Main.ParentTagName))
                {
                    return new Result { Error = "除四个顶级标签外，其他标签必须包含父标签", Successful = false };
                }
                else
                {
                    parentTag = await _tagRepository.FirstOrDefaultAsync(s => s.Name == model.Main.ParentTagName);
                    if (parentTag == null)
                    {
                        return new Result { Error = "父标签必须真实存在", Successful = false };
                    }
                }
                //预处理 建立子标签关联信息
                //判断关联是否存在
                var tagIds = new List<int>();

                var tagNames = new List<string>();
                tagNames.AddRange(model.Tags.Tags.Where(s => string.IsNullOrWhiteSpace(s.DisplayName) == false).Select(s => s.DisplayName));

                try
                {
                    tagIds = await _tagService.GetTagIdsFromNames(tagNames);
                }
                catch (Exception ex)
                {
                    return new Result { Successful = false, Error = ex.Message };
                }
                //获取标签
                var tags = await _tagRepository.GetAll().Where(s => tagIds.Contains(s.Id)).ToListAsync();

                //判断关联是否存在
                var entryIds = new List<int>();

                var entryNames = new List<string>();
                entryNames.AddRange(model.Entries.Entries.Where(s => string.IsNullOrWhiteSpace(s.DisplayName) == false).Select(s => s.DisplayName));
                try
                {
                    entryIds = await _entryService.GetEntryIdsFromNames(entryNames);
                }
                catch (Exception ex)
                {
                    return new Result { Successful = false, Error = ex.Message };
                }
                //获取词条
                var entries = await _entryRepository.GetAll().Where(s => entryIds.Contains(s.Id)).ToListAsync();


                var newTag = new Tag();

                _tagService.SetDataFromEditTagMainViewModel(newTag, model.Main, parentTag);
                _tagService.SetDataFromEditTagChildEntriesViewModel(newTag, model.Entries, entries);
                _tagService.SetDataFromEditTagChildTagsViewModel(newTag, model.Tags, tags);

                var tag = new Tag();
                //获取审核记录
                try
                {
                    tag = await _examineService.AddNewTagExaminesAsync(newTag, user, model.Note);
                }
                catch (Exception ex)
                {
                    return new Result { Successful = false, Error = ex.Message };

                }


                return new Result { Successful = true, Error = tag.Id.ToString() };
            }
            catch (Exception)
            {
                return new Result { Error = "创建标签的过程中发生未知错误，请确保数据格式正确后联系管理员", Successful = false };
            }

        }

        [HttpGet("{id}")]
        public async Task<ActionResult<EditTagMainViewModel>> EditMainAsync(int id)
        {
            //获取当前用户ID
            var user = await _appHelper.GetAPICurrentUserAsync(HttpContext);

            //判断是否为锁定状态
            if (await _examineRepository.GetAll().AsNoTracking().AnyAsync(s => s.ApplicationUserId != user.Id && s.Id == id && s.IsPassed == null && (s.Operation == Operation.EditTagMain)))
            {
                return NotFound("当前标签该部分已经被另一名用户编辑，正在等待审核，请等待审核结束后再进行编辑");
            }


            //获取标签
            var tag = await _tagRepository.GetAll().Include(s => s.ParentCodeNavigation).FirstOrDefaultAsync(s => s.Id == id && s.IsHidden != true);
            if (tag == null)
            {
                return NotFound();
            }

            //获取审核记录
            var examine = await _examineService.GetUserTagActiveExamineAsync(tag.Id, user.Id, Operation.EditTagMain);
            if (examine != null)
            {
                await _tagService.UpdateTagDataAsync(tag, examine);
            }


            var model = new EditTagMainViewModel
            {
                Id = tag.Id,
                Name = tag.Name,
                BriefIntroduction = tag.BriefIntroduction,
                MainPicture = _appHelper.GetImagePath(tag.MainPicture, ""),
                BackgroundPicture = _appHelper.GetImagePath(tag.BackgroundPicture, ""),
                SmallBackgroundPicture = _appHelper.GetImagePath(tag.SmallBackgroundPicture, ""),
                Thumbnail = _appHelper.GetImagePath(tag.Thumbnail, ""),
                ParentTagName = tag.ParentCodeNavigation?.Name ?? ""
            };


            return model;
        }

        [HttpPost]
        public async Task<ActionResult<Result>> EditMainAsync(EditTagMainViewModel model)
        {
            //获取当前用户ID
            var user = await _appHelper.GetAPICurrentUserAsync(HttpContext);
            //检查是否超过编辑上限
            if (await _examineRepository.CountAsync(s => s.ApplicationUserId == user.Id && s.IsPassed == null) > ToolHelper.MaxEditorCount)
            {
                return new Result { Successful = false, Error = "当前已超过最大待审核编辑数目，请等待审核通过后继续编辑，长时间未更新请联系管理员" };
            }
            //判断是否为锁定状态
            if (await _examineRepository.GetAll().AsNoTracking().AnyAsync(s => s.ApplicationUserId != user.Id && s.Id == model.Id && s.IsPassed == null && (s.Operation == Operation.EditTagMain)))
            {
                return new Result { Error = "当前标签该部分已经被另一名用户编辑，正在等待审核,请等待审核结束后再进行编辑", Successful = false };
            }


            //判断父标签是否存在
            Tag parentTag = null;
            if (model.Name != "游戏" && model.Name != "角色" && model.Name != "制作组" && model.Name != "STAFF")
            {
                if (string.IsNullOrWhiteSpace(model.ParentTagName))
                {
                    return new Result { Error = "除四个顶级标签外，其他标签必须包含父标签", Successful = false };
                }
                else
                {
                    parentTag = await _tagRepository.FirstOrDefaultAsync(s => s.Name == model.ParentTagName);
                    if (parentTag == null)
                    {
                        return new Result { Error = "父标签必须真实存在", Successful = false };
                    }
                }
            }

            //查找当前标签
            var currentTag = await _tagRepository.GetAll().Include(s => s.ParentCodeNavigation).FirstOrDefaultAsync(s => s.Id == model.Id);
            var newTag = await _tagRepository.GetAll().AsNoTracking().Include(s => s.ParentCodeNavigation).FirstOrDefaultAsync(s => s.Id == model.Id);
            if (currentTag == null)
            {
                return new Result { Error = $"无法找到ID为{model.Id}的标签", Successful = false };
            }

            _tagService.SetDataFromEditTagMainViewModel(newTag, model, parentTag);


            var examines = _tagService.ExaminesCompletion(currentTag, newTag);

            if (examines.Any(s => s.Value == Operation.EditTagMain) == false)
            {
                return new Result { Successful = true };
            }
            var examine = examines.FirstOrDefault(s => s.Value == Operation.EditTagMain);
            //序列化
            var resulte = "";
            using (TextWriter text = new StringWriter())
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(text, examine.Key);
                resulte = text.ToString();
            }
            //判断是否是管理员
            if (await _userManager.IsInRoleAsync(user, "Editor") == true)
            {
                await _examineService.ExamineEditTagMainAsync(currentTag, examine.Key as ExamineMain);
                await _examineService.UniversalEditTagExaminedAsync(currentTag, user, true, resulte, Operation.EditTagMain, model.Note);
                await _appHelper.AddUserContributionValueAsync(user.Id, currentTag.Id, Operation.EditTagMain);
            }
            else
            {
                await _examineService.UniversalEditTagExaminedAsync(currentTag, user, false, resulte, Operation.EditTagMain, model.Note);
            }


            return new Result { Successful = true };

        }

        [HttpGet("{id}")]
        public async Task<ActionResult<EditTagChildTagsViewModel>> EditChildTags(int id)
        {
            //获取当前用户ID
            var user = await _appHelper.GetAPICurrentUserAsync(HttpContext);

            //获取周边
            var tag = await _tagRepository.GetAll().Include(s => s.InverseParentCodeNavigation).FirstOrDefaultAsync(s => s.Id == id && s.IsHidden != true);
            if (tag == null)
            {
                return NotFound();
            }

            //判断是否为锁定状态
            if (await _examineRepository.GetAll().AsNoTracking().AnyAsync(s => s.ApplicationUserId != user.Id && s.Id == id && s.IsPassed == null && (s.Operation == Operation.EditTagChildTags)))
            {
                return NotFound("当前标签该部分已经被另一名用户编辑，正在等待审核,请等待审核结束后再进行编辑");
            }

            //获取审核记录
            var examine = await _examineService.GetUserTagActiveExamineAsync(tag.Id, user.Id, Operation.EditTagChildTags);
            if (examine != null)
            {
                await _tagService.UpdateTagDataAsync(tag, examine);
            }

            //处理附加信息
            var model = new EditTagChildTagsViewModel
            {
                Id = id,
                Name = tag.Name
            };
            var tags = tag.InverseParentCodeNavigation;

            foreach (var item in tags)
            {
                model.Tags.Add(new RelevancesModel
                {
                    DisplayName = item.Name
                });
            }

            return model;
        }

        [HttpPost]
        public async Task<ActionResult<Result>> EditChildTags(EditTagChildTagsViewModel model)
        {
            //获取当前用户ID
            var user = await _appHelper.GetAPICurrentUserAsync(HttpContext);
            //检查是否超过编辑上限
            if (await _examineRepository.CountAsync(s => s.ApplicationUserId == user.Id && s.IsPassed == null) > ToolHelper.MaxEditorCount)
            {
                return new Result { Successful = false, Error = "当前已超过最大待审核编辑数目，请等待审核通过后继续编辑，长时间未更新请联系管理员" };
            }
            //判断是否为锁定状态
            if (await _examineRepository.GetAll().AsNoTracking().AnyAsync(s => s.ApplicationUserId != user.Id && s.Id == model.Id && s.IsPassed == null && (s.Operation == Operation.EditTagChildTags)))
            {
                return new Result { Error = "当前标签该部分已经被另一名用户编辑，正在等待审核,请等待审核结束后再进行编辑", Successful = false };
            }
            //预处理 建立词条关联信息
            //判断关联是否存在
            var tagIds = new List<int>();

            var tagNames = new List<string>();
            tagNames.AddRange(model.Tags.Where(s => string.IsNullOrWhiteSpace(s.DisplayName) == false).Select(s => s.DisplayName));

            try
            {
                tagIds = await _tagService.GetTagIdsFromNames(tagNames);
            }
            catch (Exception ex)
            {
                return new Result { Successful = false, Error = ex.Message };
            }
            //获取标签
            var tags = await _tagRepository.GetAll().Where(s => tagIds.Contains(s.Id)).ToListAsync();

            //查找当前周边
            var currentTag = await _tagRepository.GetAll().Include(s => s.InverseParentCodeNavigation).FirstOrDefaultAsync(s => s.Id == model.Id);
            var newTag = await _tagRepository.GetAll().AsNoTracking().Include(s => s.InverseParentCodeNavigation).FirstOrDefaultAsync(s => s.Id == model.Id);
            if (currentTag == null)
            {
                return new Result { Error = $"无法找到ID为{model.Id}的标签", Successful = false };
            }

            _tagService.SetDataFromEditTagChildTagsViewModel(newTag, model, tags);

            var examines = _tagService.ExaminesCompletion(currentTag, newTag);

            if (examines.Any(s => s.Value == Operation.EditTagChildTags) == false)
            {
                return new Result { Successful = true };
            }
            var examine = examines.FirstOrDefault(s => s.Value == Operation.EditTagChildTags);
            //序列化
            var resulte = "";
            using (TextWriter text = new StringWriter())
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(text, examine.Key);
                resulte = text.ToString();
            }
            //判断是否是管理员
            if (await _userManager.IsInRoleAsync(user, "Editor") == true)
            {
                await _examineService.ExamineEditTagChildTagsAsync(currentTag, examine.Key as TagChildTags);
                await _examineService.UniversalEditTagExaminedAsync(currentTag, user, true, resulte, Operation.EditTagChildTags, model.Note);
                await _appHelper.AddUserContributionValueAsync(user.Id, currentTag.Id, Operation.EditTagChildTags);
            }
            else
            {
                await _examineService.UniversalEditTagExaminedAsync(currentTag, user, false, resulte, Operation.EditTagChildTags, model.Note);
            }


            return new Result { Successful = true };

        }

        [HttpGet("{id}")]
        public async Task<ActionResult<EditTagChildEntriesViewModel>> EditChildEntries(int id)
        {
            //获取当前用户ID
            var user = await _appHelper.GetAPICurrentUserAsync(HttpContext);

            //获取周边
            var tag = await _tagRepository.GetAll().Include(s => s.Entries).FirstOrDefaultAsync(s => s.Id == id && s.IsHidden != true);
            if (tag == null)
            {
                return NotFound();
            }

            //判断是否为锁定状态
            if (await _examineRepository.GetAll().AsNoTracking().AnyAsync(s => s.ApplicationUserId != user.Id && s.Id == id && s.IsPassed == null && (s.Operation == Operation.EditTagChildEntries)))
            {
                return NotFound("当前标签该部分已经被另一名用户编辑，正在等待审核,请等待审核结束后再进行编辑");
            }

            //获取审核记录
            var examine = await _examineService.GetUserTagActiveExamineAsync(tag.Id, user.Id, Operation.EditTagChildEntries);
            if (examine != null)
            {
                await _tagService.UpdateTagDataAsync(tag, examine);
            }

            //处理附加信息
            var model = new EditTagChildEntriesViewModel
            {
                Id = id,
                Name = tag.Name
            };
            var tags = tag.Entries;

            foreach (var item in tags)
            {
                model.Entries.Add(new RelevancesModel
                {
                    DisplayName = item.Name
                });
            }

            return model;
        }

        [HttpPost]
        public async Task<ActionResult<Result>> EditChildEntries(EditTagChildEntriesViewModel model)
        {
            //获取当前用户ID
            var user = await _appHelper.GetAPICurrentUserAsync(HttpContext);
            //检查是否超过编辑上限
            if (await _examineRepository.CountAsync(s => s.ApplicationUserId == user.Id && s.IsPassed == null) > ToolHelper.MaxEditorCount)
            {
                return new Result { Successful = false, Error = "当前已超过最大待审核编辑数目，请等待审核通过后继续编辑，长时间未更新请联系管理员" };
            }
            //判断是否为锁定状态
            if (await _examineRepository.GetAll().AsNoTracking().AnyAsync(s => s.ApplicationUserId != user.Id && s.Id == model.Id && s.IsPassed == null && (s.Operation == Operation.EditTagChildEntries)))
            {
                return new Result { Error = "当前标签该部分已经被另一名用户编辑，正在等待审核,请等待审核结束后再进行编辑", Successful = false };
            }
            //预处理 建立词条关联信息
            //判断关联是否存在
            var entryIds = new List<int>();

            var entryNames = new List<string>();
            entryNames.AddRange(model.Entries.Where(s => string.IsNullOrWhiteSpace(s.DisplayName) == false).Select(s => s.DisplayName));

            try
            {
                entryIds = await _entryService.GetEntryIdsFromNames(entryNames);
            }
            catch (Exception ex)
            {
                return new Result { Successful = false, Error = ex.Message };
            }
            //获取词条
            var entries = await _entryRepository.GetAll().Where(s => entryIds.Contains(s.Id)).ToListAsync();

            //查找当前标签
            var currentTag = await _tagRepository.GetAll().Include(s => s.Entries).FirstOrDefaultAsync(s => s.Id == model.Id);
            var newTag = await _tagRepository.GetAll().AsNoTracking().Include(s => s.Entries).FirstOrDefaultAsync(s => s.Id == model.Id);
            if (currentTag == null)
            {
                return new Result { Error = $"无法找到ID为{model.Id}的标签", Successful = false };
            }

            _tagService.SetDataFromEditTagChildEntriesViewModel(newTag, model, entries);

            var examines = _tagService.ExaminesCompletion(currentTag, newTag);

            if (examines.Any(s => s.Value == Operation.EditTagChildEntries) == false)
            {
                return new Result { Successful = true };
            }
            var examine = examines.FirstOrDefault(s => s.Value == Operation.EditTagChildEntries);
            //序列化
            var resulte = "";
            using (TextWriter text = new StringWriter())
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(text, examine.Key);
                resulte = text.ToString();
            }
            //判断是否是管理员
            if (await _userManager.IsInRoleAsync(user, "Editor") == true)
            {
                await _examineService.ExamineEditTagChildEntriesAsync(currentTag, examine.Key as TagChildEntries);
                await _examineService.UniversalEditTagExaminedAsync(currentTag, user, true, resulte, Operation.EditTagChildEntries, model.Note);
                await _appHelper.AddUserContributionValueAsync(user.Id, currentTag.Id, Operation.EditTagChildEntries);
            }
            else
            {
                await _examineService.UniversalEditTagExaminedAsync(currentTag, user, false, resulte, Operation.EditTagChildEntries, model.Note);
            }

            return new Result { Successful = true };

        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<string>>> GetAllTagsAsync()
        {
            return await _tagRepository.GetAll().AsNoTracking().Where(s => s.IsHidden != true && string.IsNullOrWhiteSpace(s.Name) == false).Select(b => b.Name).ToListAsync();

        }

        [HttpPost]
        public async Task<ActionResult<BootstrapBlazor.Components.QueryData<ListTagAloneModel>>> GetTagListAsync(TagsPagesInfor input)
        {
            var dtos = await _tagService.GetPaginatedResult(input.Options, input.SearchModel);

            return dtos;
        }

        [HttpGet]
        public async Task<ActionResult<ListTagsInforViewModel>> ListTagsAsync()
        {
            var model = new ListTagsInforViewModel
            {
                All = await _tagRepository.CountAsync(),
                Hiddens = await _tagRepository.CountAsync(s => s.IsHidden == true)
            };

            return model;
        }


        [HttpPost]
        public async Task<ActionResult<Result>> RevokeExamine(RevokeExamineModel model)
        {
            //获取当前用户ID
            var user = await _appHelper.GetAPICurrentUserAsync(HttpContext);
            //查找审核
            var examine = await _examineRepository.FirstOrDefaultAsync(s => s.TagId == model.Id && s.ApplicationUserId == user.Id && s.Operation == model.ExamineType && s.IsPassed == null);
            if (examine != null)
            {
                await _examineRepository.DeleteAsync(examine);
                //删除以此审核为前置审核的
                await _examineRepository.DeleteAsync(s => s.PrepositionExamineId == examine.Id);
                return new Result { Successful = true };
            }
            else
            {
                return new Result { Successful = false, Error = "找不到目标审核记录" };
            }
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        public async Task<ActionResult<Result>> HiddenTagAsync(HiddenTagModel model)
        {
            await _tagRepository.GetRangeUpdateTable().Where(s => model.Ids.Contains(s.Id)).Set(s => s.IsHidden, b => model.IsHidden).ExecuteAsync();

            return new Result { Successful = true };
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<EditTagInforBindModel>> GetTagEditInforBindModelAsync(long id)
        {
            //获取当前用户ID
            var user = await _appHelper.GetAPICurrentUserAsync(HttpContext);

            var tag = await _tagRepository.FirstOrDefaultAsync(s => s.Id == id);
            if (tag == null)
            {
                return NotFound("无法找到该标签");
            }
            var model = new EditTagInforBindModel
            {
                Id = id,
                Name = tag.Name
            };

            //获取编辑记录
            model.Examines = await _examineService.GetExaminesToNormalListAsync(_examineRepository.GetAll().Where(s => s.TagId == id && (s.IsPassed == true || (user != null && s.IsPassed == null && s.ApplicationUserId == user.Id))), true);
            model.Examines = model.Examines.OrderByDescending(s => s.ApplyTime).ToList();
            //获取编辑状态
            model.State = await _tagService.GetTagEditState(user, id);

            return model;
        }

        /// <summary>
        /// 获取编辑记录概览
        /// </summary>
        /// <param name="contrastId">要对比的编辑记录</param>
        /// <param name="currentId">当前最新的编辑记录</param>
        /// <returns></returns>
        [HttpGet("{contrastId}/{currentId}")]
        [AllowAnonymous]
        public async Task<ActionResult<TagContrastEditRecordViewModel>> GetContrastEditRecordViewsAsync(long contrastId, long currentId)
        {
            if (contrastId > currentId)
            {
                return BadRequest("对比的编辑必须先于当前的编辑");
            }

            var contrastExamine = await _examineRepository.FirstOrDefaultAsync(s => s.Id == contrastId);
            var currentExamine = await _examineRepository.FirstOrDefaultAsync(s => s.Id == currentId);

            if (contrastExamine == null || currentExamine == null || contrastExamine.TagId == null || currentExamine.TagId == null || contrastExamine.TagId != currentExamine.TagId)
            {
                return NotFound("编辑记录Id不正确");
            }

            var currentTag = new Tag();
            var newTag = new Tag();

            //获取审核记录
            var examines = await _examineRepository.GetAll().AsNoTracking().Where(s => s.IsPassed == true && s.TagId == currentExamine.TagId).ToListAsync();

            foreach (var item in examines.Where(s => s.Id <= contrastId))
            {
                await _tagService.UpdateTagDataAsync(currentTag, item);
            }

            foreach (var item in examines.Where(s => s.Id <= currentId))
            {
                await _tagService.UpdateTagDataAsync(newTag, item);
            }

            var result = await _tagService.ConcompareAndGenerateModel(currentTag, newTag);

            var model = new TagContrastEditRecordViewModel
            {
                ContrastId = contrastId,
                CurrentId = currentId,
                ContrastModel = result[0],
                CurrentModel = result[1],
            };

            return model;
        }


        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<List<RandomTagModel>>> GetRandomTagsAsync()
        {
            var tags = await _tagRepository.GetAll().AsNoTracking()
                .Include(s => s.Entries)
                .Where(s => s.Entries.Count > 3 && s.Name != "免费")
                .ToListAsync();

            var model = new List<RandomTagModel>();
            foreach (var item in tags)
            {
                var temp = new RandomTagModel
                {
                    Id = item.Id,
                    Name = item.Name
                };
                foreach (var infor in item.Entries.ToList().Random().Take(12))
                {
                    temp.Entries.Add(await _appHelper.GetEntryInforTipViewModel(infor));
                }
                model.Add(temp);
            }

            return model;
        }

    }
}
