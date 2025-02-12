﻿using CnGalWebSite.APIServer.Application.Helper;
using CnGalWebSite.APIServer.Application.Ranks;
using CnGalWebSite.APIServer.DataReositories;
using CnGalWebSite.APIServer.ExamineX;
using CnGalWebSite.DataModel.ExamineModel;
using CnGalWebSite.DataModel.Helper;
using CnGalWebSite.DataModel.Model;
using CnGalWebSite.DataModel.ViewModel.Admin;
using CnGalWebSite.DataModel.ViewModel.Home;
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
using System.Linq.Dynamic.Core;

namespace CnGalWebSite.APIServer.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    [Route("api/examines/[action]")]
    [ApiController]
    public class ExaminesAPIController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRepository<Entry, int> _entryRepository;
        private readonly IRepository<Examine, long> _examineRepository;
        private readonly IRepository<Tag, int> _tagRepository;
        private readonly IRepository<Article, long> _articleRepository;
        private readonly IRepository<Disambig, int> _disambigRepository;
        private readonly IRepository<Message, long> _messageRepository;
        private readonly IRepository<Comment, long> _commentRepository;
        private readonly IRepository<Periphery, long> _peripheryRepository;
        private readonly IRepository<PlayedGame, long> _playedGameRepository;
        private readonly IRepository<ApplicationUser, string> _userRepository;
        private readonly IAppHelper _appHelper;
        private readonly IExamineService _examineService;
        private readonly IRankService _rankService;


        public ExaminesAPIController(IRepository<Disambig, int> disambigRepository, IRankService rankService, IRepository<Comment, long> commentRepository,
        IRepository<Message, long> messageRepository, IRepository<ApplicationUser, string> userRepository,
        UserManager<ApplicationUser> userManager, IExamineService examineService,
        IRepository<Article, long> articleRepository, IAppHelper appHelper, IRepository<Entry, int> entryRepository, IRepository<Periphery, long> peripheryRepository, IRepository<Examine, long> examineRepository, IRepository<Tag, int> tagRepository, IRepository<PlayedGame, long> playedGameRepository)
        {
            _userManager = userManager;
            _entryRepository = entryRepository;
            _examineRepository = examineRepository;
            _tagRepository = tagRepository;
            _appHelper = appHelper;
            _articleRepository = articleRepository;
            _examineService = examineService;
            _messageRepository = messageRepository;
            _commentRepository = commentRepository;
            _disambigRepository = disambigRepository;
            _rankService = rankService;
            _peripheryRepository = peripheryRepository;
            _playedGameRepository = playedGameRepository;
            _userRepository = userRepository;
        }



        /// <summary>
        /// 获取编辑记录详细信息视图
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("{id}")]
        [HttpGet]
        public async Task<ActionResult<ExamineViewModel>> GetExamineViewAsync(long id)
        {
            //获取当前用户ID
            var user = await _appHelper.GetAPICurrentUserAsync(HttpContext);
            //获取审核单
            Examine examine =
                examine = await _examineRepository.GetAll()
                        .Include(s => s.ApplicationUser)
                        .FirstOrDefaultAsync(x => x.Id == id);


            if (examine == null)
            {
                return NotFound();
            }
            //对应赋值
            var model = new ExamineViewModel
            {
                Id = examine.Id,
                UserId = examine.ApplicationUserId,
                ApplyTime = examine.ApplyTime,
                Operation = examine.Operation,
                UserName = examine.ApplicationUser.UserName,
                Comments = examine.Comments,
                IsPassed = false,
                PassedAdminName = examine.PassedAdminName,
                Note = examine.Note
            };
            //判断是否有前置审核
            if (examine.PrepositionExamineId != null)
            {
                var temp = await _examineRepository.FirstOrDefaultAsync(s => s.Id == examine.PrepositionExamineId && s.IsPassed == null);
                if (temp == null)
                {
                    model.PrepositionExamineId = -1;
                }
                else
                {
                    model.PrepositionExamineId = (int)examine.PrepositionExamineId;
                }
            }

            model.PassedTime = examine.PassedTime;
            model.IsPassed = examine.IsPassed;

            try
            {
                if (await _examineService.GetExamineView(model, examine) == false)
                {
                    return NotFound();
                }

            }
            catch (Exception)
            {
                return NotFound("生成审核视图出错");
            }
            //获取敏感词列表
            //if (string.IsNullOrWhiteSpace(examine.Context) == false && user != null && await _userManager.IsInRoleAsync(user, "Admin"))
            //{
            //    model.SensitiveWords = await _appHelper.GetSensitiveWordsInText(examine.Context);
            //}
            return model;
        }

        /// <summary>
        /// 对编辑进行审核
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<Result>> ProcAsync(Models.ExaminedViewModel model)
        {
            var examine = await _examineRepository.FirstOrDefaultAsync(x => x.Id == model.Id);
            if (examine == null)
            {
                return NotFound();
            }
            if (examine.IsPassed != null)
            {
                return new Result { Successful = false, Error = "该记录已经被被审核，不能修改审核状态" };
            }
            var user = await _userRepository.GetAll().AsNoTracking().FirstOrDefaultAsync(s => s.Id == examine.ApplicationUserId);
            if (user == null)
            {
                return NotFound();
            }
            //判断是否有前置审核
            if (examine.PrepositionExamineId != null)
            {
                var temp = await _examineRepository.FirstOrDefaultAsync(s => s.Id == examine.PrepositionExamineId && s.IsPassed == null);
                if (temp != null)
                {
                    return new Result { Successful = false, Error = $"该审核有一个前置审核,请先对前置审核进行审核，ID{examine.PrepositionExamineId}" };
                }
            }
            Entry entry = null;
            Article article = null;
            Comment comment = null;
            Tag tag = null;
            Disambig disambig = null;
            Periphery periphery = null;
            PlayedGame playedGame = null;
            //获取当前管理员ID
            var userAdmin = await _appHelper.GetAPICurrentUserAsync(HttpContext);

            //判断是否通过
            if (model.IsPassed == true)
            {

                //根据操作类别进行操作
                switch (examine.Operation)
                {
                    case Operation.UserMainPage:
                        user = await _userRepository.GetAll().FirstOrDefaultAsync(s => s.Id == examine.ApplicationUserId);
                        await _examineService.ExamineEditUserMainPageAsync(user, examine.Context);
                        break;
                    case Operation.EditUserMain:
                        user = await _userRepository.GetAll().FirstOrDefaultAsync(s => s.Id == examine.ApplicationUserId);
                        //序列化数据
                        UserMain userMain = null;
                        using (TextReader str = new StringReader(examine.Context))
                        {
                            var serializer = new JsonSerializer();
                            userMain = (UserMain)serializer.Deserialize(str, typeof(UserMain));
                        }

                        await _examineService.ExamineEditUserMainAsync(user, userMain);
                        break;
                    case Operation.EstablishMain:
                        entry = await _entryRepository.FirstOrDefaultAsync(s => s.Id == examine.EntryId);
                        if (entry == null)
                        {
                            return NotFound();
                        }
                        //序列化数据
                        ExamineMain examineMain = null;
                        using (TextReader str = new StringReader(examine.Context))
                        {
                            var serializer = new JsonSerializer();
                            examineMain = (ExamineMain)serializer.Deserialize(str, typeof(ExamineMain));
                        }

                        await _examineService.ExamineEstablishMainAsync(entry, examineMain);
                        break;
                    case Operation.EstablishAddInfor:
                        try
                        {
                            entry = await _entryRepository.GetAll()
                                                       .Include(s => s.Information).ThenInclude(s => s.Additional)
                                                       .Include(s => s.EntryRelationFromEntryNavigation).ThenInclude(s => s.ToEntryNavigation)
                                                       .FirstOrDefaultAsync(s => s.Id == examine.EntryId);
                            if (entry == null)
                            {
                                return NotFound();
                            }

                            //读取当前用户等待审核的信息
                            //序列化数据
                            EntryAddInfor entryAddInfor = null;
                            using (TextReader str = new StringReader(examine.Context))
                            {
                                var serializer = new JsonSerializer();
                                entryAddInfor = (EntryAddInfor)serializer.Deserialize(str, typeof(EntryAddInfor));
                            }
                            await _examineService.ExamineEstablishAddInforAsync(entry, entryAddInfor);
                        }
                        catch (Exception)
                        {

                        }

                        break;
                    case Operation.EstablishImages:
                        entry = await _entryRepository.GetAll()
                           .Include(s => s.Pictures)
                           .FirstOrDefaultAsync(s => s.Id == examine.EntryId);
                        if (entry == null)
                        {
                            return NotFound();
                        }

                        //序列化数据
                        EntryImages entryImages = null;
                        using (TextReader str = new StringReader(examine.Context))
                        {
                            var serializer = new JsonSerializer();
                            entryImages = (EntryImages)serializer.Deserialize(str, typeof(EntryImages));
                        }

                        await _examineService.ExamineEstablishImagesAsync(entry, entryImages);
                        break;
                    case Operation.EstablishRelevances:

                        entry = await _entryRepository.GetAll()
                    .Include(s => s.EntryRelationFromEntryNavigation).ThenInclude(s => s.ToEntryNavigation)
                            .FirstOrDefaultAsync(s => s.Id == examine.EntryId);
                        if (entry == null)
                        {
                            return NotFound();
                        }

                        EntryRelevances entryRelevances = null;
                        using (TextReader str = new StringReader(examine.Context))
                        {
                            var serializer = new JsonSerializer();
                            entryRelevances = (EntryRelevances)serializer.Deserialize(str, typeof(EntryRelevances));
                        }
                        await _examineService.ExamineEstablishRelevancesAsync(entry, entryRelevances);
                        break;
                    case Operation.EstablishTags:

                        entry = await _entryRepository.GetAll().Include(s => s.Tags).FirstOrDefaultAsync(s => s.Id == examine.EntryId);
                        if (entry == null)
                        {
                            return NotFound();
                        }
                        //序列化数据
                        EntryTags entryTags = null;
                        using (TextReader str = new StringReader(examine.Context))
                        {
                            var serializer = new JsonSerializer();
                            entryTags = (EntryTags)serializer.Deserialize(str, typeof(EntryTags));
                        }
                        await _examineService.ExamineEstablishTagsAsync(entry, entryTags);
                        break;
                    case Operation.EstablishMainPage:

                        entry = await _entryRepository.FirstOrDefaultAsync(s => s.Id == examine.EntryId);
                        if (entry == null)
                        {
                            return NotFound();
                        }
                        await _examineService.ExamineEstablishMainPageAsync(entry, examine.Context);
                        break;
                    case Operation.EditArticleMain:

                        article = await _articleRepository.FirstOrDefaultAsync(s => s.Id == examine.ArticleId);
                        if (article == null)
                        {
                            return NotFound();
                        }
                        //序列化数据
                        ExamineMain articleMain = null;
                        using (TextReader str = new StringReader(examine.Context))
                        {
                            var serializer = new JsonSerializer();
                            articleMain = (ExamineMain)serializer.Deserialize(str, typeof(ExamineMain));
                        }

                        await _examineService.ExamineEditArticleMainAsync(article, articleMain);
                        break;
                    case Operation.EditArticleRelevanes:

                        article = await _articleRepository.GetAll()
                            .Include(s => s.ArticleRelationFromArticleNavigation).ThenInclude(s => s.ToArticleNavigation)
                            .FirstOrDefaultAsync(s => s.Id == examine.ArticleId);
                        if (article == null)
                        {
                            return NotFound();
                        }

                        ArticleRelevances articleRelevances = null;
                        using (TextReader str = new StringReader(examine.Context))
                        {
                            var serializer = new JsonSerializer();
                            articleRelevances = (ArticleRelevances)serializer.Deserialize(str, typeof(ArticleRelevances));
                        }
                        await _examineService.ExamineEditArticleRelevancesAsync(article, articleRelevances);
                        break;
                    case Operation.EditArticleMainPage:

                        article = await _articleRepository.FirstOrDefaultAsync(s => s.Id == examine.ArticleId);
                        if (article == null)
                        {
                            return NotFound();
                        }
                        await _examineService.ExamineEditArticleMainPageAsync(article, examine.Context);
                        break;
                    case Operation.EditTagMain:
                        tag = await _tagRepository.GetAll().Include(s => s.ParentCodeNavigation).FirstOrDefaultAsync(s => s.Id == examine.TagId);
                        if (tag == null)
                        {
                            return NotFound();
                        }
                        ExamineMain tagMain = null;
                        using (TextReader str = new StringReader(examine.Context))
                        {
                            var serializer = new JsonSerializer();
                            tagMain = (ExamineMain)serializer.Deserialize(str, typeof(ExamineMain));
                        }
                        await _examineService.ExamineEditTagMainAsync(tag, tagMain);
                        break;
                    case Operation.EditTagChildTags:
                        tag = await _tagRepository.GetAll().Include(s => s.InverseParentCodeNavigation).FirstOrDefaultAsync(s => s.Id == examine.TagId);
                        if (tag == null)
                        {
                            return NotFound();
                        }
                        TagChildTags tagChildTags = null;
                        using (TextReader str = new StringReader(examine.Context))
                        {
                            var serializer = new JsonSerializer();
                            tagChildTags = (TagChildTags)serializer.Deserialize(str, typeof(TagChildTags));
                        }
                        await _examineService.ExamineEditTagChildTagsAsync(tag, tagChildTags);
                        break;
                    case Operation.EditTagChildEntries:
                        tag = await _tagRepository.GetAll().Include(s => s.Entries).FirstOrDefaultAsync(s => s.Id == examine.TagId);
                        if (tag == null)
                        {
                            return NotFound();
                        }
                        TagChildEntries tagChildEntries = null;
                        using (TextReader str = new StringReader(examine.Context))
                        {
                            var serializer = new JsonSerializer();
                            tagChildEntries = (TagChildEntries)serializer.Deserialize(str, typeof(TagChildEntries));
                        }
                        await _examineService.ExamineEditTagChildEntriesAsync(tag, tagChildEntries);
                        break;
                    case Operation.PubulishComment:
                        comment = await _commentRepository.GetAll()
                            .Include(s => s.ParentCodeNavigation)
                            .FirstOrDefaultAsync(s => s.Id == examine.CommentId);
                        if (comment == null)
                        {
                            return NotFound();
                        }
                        CommentText commentText = null;
                        using (TextReader str = new StringReader(examine.Context))
                        {
                            var serializer = new JsonSerializer();
                            commentText = (CommentText)serializer.Deserialize(str, typeof(CommentText));
                        }
                        await _examineService.ExaminePublishCommentTextAsync(comment, commentText);
                        break;
                    case Operation.DisambigMain:
                        disambig = await _disambigRepository.FirstOrDefaultAsync(s => s.Id == examine.DisambigId);
                        if (disambig == null)
                        {
                            return NotFound();
                        }
                        //序列化数据
                        DisambigMain disambigMain = null;
                        using (TextReader str = new StringReader(examine.Context))
                        {
                            var serializer = new JsonSerializer();
                            disambigMain = (DisambigMain)serializer.Deserialize(str, typeof(DisambigMain));
                        }

                        await _examineService.ExamineEditDisambigMainAsync(disambig, disambigMain);
                        break;
                    case Operation.DisambigRelevances:
                        disambig = await _disambigRepository.FirstOrDefaultAsync(s => s.Id == examine.DisambigId);
                        if (disambig == null)
                        {
                            return NotFound();
                        }
                        //序列化数据
                        DisambigRelevances disambigRelevances = null;
                        using (TextReader str = new StringReader(examine.Context))
                        {
                            var serializer = new JsonSerializer();
                            disambigRelevances = (DisambigRelevances)serializer.Deserialize(str, typeof(DisambigRelevances));
                        }

                        await _examineService.ExamineEditDisambigRelevancesAsync(disambig, disambigRelevances);
                        break;
                    case Operation.EditPeripheryMain:
                        periphery = await _peripheryRepository.FirstOrDefaultAsync(s => s.Id == examine.PeripheryId);
                        if (periphery == null)
                        {
                            return NotFound();
                        }
                        //序列化数据
                        ExamineMain peripheryMain = null;
                        using (TextReader str = new StringReader(examine.Context))
                        {
                            var serializer = new JsonSerializer();
                            peripheryMain = (ExamineMain)serializer.Deserialize(str, typeof(ExamineMain));
                        }

                        await _examineService.ExamineEditPeripheryMainAsync(periphery, peripheryMain);
                        break;
                    case Operation.EditPeripheryImages:
                        periphery = await _peripheryRepository.GetAll().Include(s => s.Pictures).FirstOrDefaultAsync(s => s.Id == examine.PeripheryId);
                        if (periphery == null)
                        {
                            return NotFound();
                        }
                        //序列化数据
                        PeripheryImages peripheryImages = null;
                        using (TextReader str = new StringReader(examine.Context))
                        {
                            var serializer = new JsonSerializer();
                            peripheryImages = (PeripheryImages)serializer.Deserialize(str, typeof(PeripheryImages));
                        }

                        await _examineService.ExamineEditPeripheryImagesAsync(periphery, peripheryImages);
                        break;
                    case Operation.EditPeripheryRelatedEntries:
                        periphery = await _peripheryRepository.GetAll()
                            .Include(s => s.RelatedEntries)
                            .FirstOrDefaultAsync(s => s.Id == examine.PeripheryId);
                        if (periphery == null)
                        {
                            return NotFound();
                        }
                        //序列化数据
                        PeripheryRelatedEntries peripheryRelevances = null;
                        using (TextReader str = new StringReader(examine.Context))
                        {
                            var serializer = new JsonSerializer();
                            peripheryRelevances = (PeripheryRelatedEntries)serializer.Deserialize(str, typeof(PeripheryRelatedEntries));
                        }

                        await _examineService.ExamineEditPeripheryRelatedEntriesAsync(periphery, peripheryRelevances);
                        break;
                    case Operation.EditPeripheryRelatedPeripheries:
                        periphery = await _peripheryRepository.GetAll()
                            .Include(s => s.PeripheryRelationFromPeripheryNavigation).ThenInclude(s => s.ToPeripheryNavigation)
                            .FirstOrDefaultAsync(s => s.Id == examine.PeripheryId);
                        if (periphery == null)
                        {
                            return NotFound();
                        }
                        //序列化数据
                        PeripheryRelatedPeripheries peripheryRelatedPeripheries = null;
                        using (TextReader str = new StringReader(examine.Context))
                        {
                            var serializer = new JsonSerializer();
                            peripheryRelatedPeripheries = (PeripheryRelatedPeripheries)serializer.Deserialize(str, typeof(PeripheryRelatedPeripheries));
                        }

                        await _examineService.ExamineEditPeripheryRelatedPeripheriesAsync(periphery, peripheryRelatedPeripheries);
                        break;
                    case Operation.EditPlayedGameMain:
                        playedGame = await _playedGameRepository.GetAll()
                            .FirstOrDefaultAsync(s => s.Id == examine.PlayedGameId);
                        if (playedGame == null)
                        {
                            return NotFound();
                        }
                        //序列化数据
                        PlayedGameMain playedGameMain = null;
                        using (TextReader str = new StringReader(examine.Context))
                        {
                            var serializer = new JsonSerializer();
                            playedGameMain = (PlayedGameMain)serializer.Deserialize(str, typeof(PlayedGameMain));
                        }

                        await _examineService.ExamineEditPlayedGameMainAsync(playedGame, playedGameMain);
                        break;
                }

                //修改审核状态
                examine.Comments = model.Comments;
                examine.PassedAdminName = userAdmin.UserName;
                examine.PassedTime = DateTime.Now.ToCstTime();
                examine.IsPassed = true;
                examine.ContributionValue = model.ContributionValue;

                try
                {
                    examine = await _examineRepository.UpdateAsync(examine);

                }
                catch (Exception)
                {

                }

                //更新用户积分
                await _appHelper.UpdateUserIntegral(user);
                await _rankService.UpdateUserRanks(user);

            }
            else
            {

                //修改审核状态
                examine.Comments = model.Comments;
                examine.PassedAdminName = userAdmin.UserName;
                examine.PassedTime = DateTime.Now.ToCstTime();
                examine.IsPassed = false;
                examine = await _examineRepository.UpdateAsync(examine);
                //修改以其为前置审核的审核状态
                if (await _examineRepository.GetAll().AnyAsync(s => s.IsPassed == null && s.PrepositionExamineId == examine.Id))
                {
                    var temp = _examineRepository.GetAll().Where(s => s.IsPassed == null && s.PrepositionExamineId == examine.Id);
                    foreach (var item in temp)
                    {
                        item.IsPassed = false;
                        item.Comments = "其前置审核被驳回";
                        item.PassedTime = DateTime.Now.ToCstTime();
                        item.PassedAdminName = userAdmin.UserName;
                        await _examineRepository.UpdateAsync(item);
                    }
                }

                entry = await _entryRepository.FirstOrDefaultAsync(s => s.Id == examine.EntryId);
                article = await _articleRepository.FirstOrDefaultAsync(s => s.Id == examine.ArticleId);
                tag = await _tagRepository.FirstOrDefaultAsync(s => s.Id == examine.TagId);
                comment = await _commentRepository.FirstOrDefaultAsync(s => s.Id == examine.CommentId);
                disambig = await _disambigRepository.FirstOrDefaultAsync(s => s.Id == examine.DisambigId);
                periphery = await _peripheryRepository.FirstOrDefaultAsync(s => s.Id == examine.PeripheryId);
                playedGame = await _playedGameRepository.FirstOrDefaultAsync(s => s.Id == examine.PlayedGameId);
            }
            //给用户发送通知
            if (article != null)
            {
                if (examine.PrepositionExamineId == null || examine.PrepositionExamineId <= 0)
                {
                    //查找是否有以此为前置审核的审核 如果有 则代表第一次创建
                    if (await _examineRepository.GetAll().AnyAsync(s => s.IsPassed == null && s.PrepositionExamineId == examine.Id) )
                    {
                        await _messageRepository.InsertAsync(new Message
                        {
                            Title = (examine.IsPassed ?? false) ? "文章发布成功提醒" : "文章发布驳回提醒",
                            PostTime = DateTime.Now.ToCstTime(),
                            Image = "default/logo.png",
                            Rank = "系统",
                            Text = "你的文章『" + (article.Name ?? ($"Id:{article.Id}")) + "』被管理员『" + userAdmin.UserName + "』" + ((examine.IsPassed ?? false) ? "审核通过，已经成功发布，感谢你为CnGal资料站做出的贡献" : "驳回") + (string.IsNullOrWhiteSpace(examine.Comments) ? "" : "，批注『" + examine.Comments + "』"),
                            Link = "articles/index/" + examine.Article.Id,
                            LinkTitle = article.Name,
                            Type = (examine.IsPassed ?? false) ? MessageType.ExaminePassed : MessageType.ExamineUnPassed,
                            ApplicationUserId = user.Id
                        });
                    }
                    else
                    {
                        await _messageRepository.InsertAsync(new Message
                        {
                            Title = (examine.IsPassed ?? false) ? "编辑通过提醒" : "编辑驳回提醒",
                            PostTime = DateTime.Now.ToCstTime(),
                            Image = "default/logo.png",
                            Rank = "系统",
                            Text = "你对文章『" + (article.Name ?? ($"Id:{article.Id}")) + "』的『" + examine.Operation.GetDisplayName() + "』操作被管理员『" + userAdmin.UserName + "』" + ((examine.IsPassed ?? false) ? "通过，感谢你为CnGal资料站做出的贡献" : "驳回") + (string.IsNullOrWhiteSpace(examine.Comments) ? "" : "，批注『" + examine.Comments + "』"),
                            Link = "articles/index/" + article.Id,
                            LinkTitle = article.Name,
                            Type = (examine.IsPassed ?? false) ? MessageType.ExaminePassed : MessageType.ExamineUnPassed,
                            ApplicationUserId = user.Id
                        });
                    }
                }

            }
            else if (entry != null)
            {
                if (examine.PrepositionExamineId == null || examine.PrepositionExamineId <= 0)
                {
                    //查找是否有以此为前置审核的审核 如果有 则代表第一此创建
                    if (await _examineRepository.GetAll().AnyAsync(s => s.IsPassed == null && s.PrepositionExamineId == examine.Id) )
                    {
                        await _messageRepository.InsertAsync(new Message
                        {
                            Title = (examine.IsPassed ?? false) ? "词条创建成功提醒" : "词条创建驳回提醒",
                            PostTime = DateTime.Now.ToCstTime(),
                            Image = "default/logo.png",
                            Rank = "系统",
                            Text = "你创建的词条『" + (entry.Name ?? ($"Id:{entry.Id}")) + "』被管理员『" + userAdmin.UserName + "』" + ((examine.IsPassed ?? false) ? "审核通过，已经可以被浏览，感谢你为CnGal资料站做出的贡献" : "驳回") + (string.IsNullOrWhiteSpace(examine.Comments) ? "" : "，批注『" + examine.Comments + "』"),
                            Link = "entries/index/" + entry.Id,
                            LinkTitle = entry.Name,
                            Type = (examine.IsPassed ?? false) ? MessageType.ExaminePassed : MessageType.ExamineUnPassed,
                            ApplicationUserId = user.Id
                        });
                    }
                    else
                    {
                        await _messageRepository.InsertAsync(new Message
                        {
                            Title = (examine.IsPassed ?? false) ? "编辑通过提醒" : "编辑驳回提醒",
                            PostTime = DateTime.Now.ToCstTime(),
                            Image = "default/logo.png",
                            Rank = "系统",
                            Text = "你对词条『" + (entry.Name ?? ($"Id:{entry.Id}")) + "』的『" + examine.Operation.GetDisplayName() + "』操作被管理员『" + userAdmin.UserName + "』" + ((examine.IsPassed ?? false) ? "通过，感谢你为CnGal资料站做出的贡献" : "驳回") + (string.IsNullOrWhiteSpace(examine.Comments) ? "" : "，批注『" + examine.Comments + "』"),
                            Link = "entries/index/" + entry.Id,
                            LinkTitle = entry.Name,
                            Type = (examine.IsPassed ?? false) ? MessageType.ExaminePassed : MessageType.ExamineUnPassed,
                            ApplicationUserId = user.Id
                        });
                    }
                }
            }
            else if (periphery != null)
            {
                if (examine.PrepositionExamineId == null || examine.PrepositionExamineId <= 0)
                {
                    //查找是否有以此为前置审核的审核 如果有 则代表第一此创建
                    if (await _examineRepository.GetAll().AnyAsync(s => s.IsPassed == null && s.PrepositionExamineId == examine.Id) )
                    {
                        await _messageRepository.InsertAsync(new Message
                        {
                            Title = (examine.IsPassed ?? false) ? "周边创建成功提醒" : "周边创建驳回提醒",
                            PostTime = DateTime.Now.ToCstTime(),
                            Image = "default/logo.png",
                            Rank = "系统",
                            Text = "你创建的周边『" + (periphery.Name ?? ($"Id:{periphery.Id}")) + "』被管理员『" + userAdmin.UserName + "』" + ((examine.IsPassed ?? false) ? "审核通过，已经可以被浏览，感谢你为CnGal资料站做出的贡献" : "驳回") + (string.IsNullOrWhiteSpace(examine.Comments) ? "" : "，批注『" + examine.Comments + "』"),
                            Link = "peripheries/index/" + periphery.Id,
                            LinkTitle = periphery.Name,
                            Type = (examine.IsPassed ?? false) ? MessageType.ExaminePassed : MessageType.ExamineUnPassed,
                            ApplicationUserId = user.Id
                        });
                    }
                    else
                    {
                        await _messageRepository.InsertAsync(new Message
                        {
                            Title = (examine.IsPassed ?? false) ? "编辑通过提醒" : "编辑驳回提醒",
                            PostTime = DateTime.Now.ToCstTime(),
                            Image = "default/logo.png",
                            Rank = "系统",
                            Text = "你对周边『" + (periphery.Name ?? ($"Id:{periphery.Id}")) + "』的『" + examine.Operation.GetDisplayName() + "』操作被管理员『" + userAdmin.UserName + "』" + ((examine.IsPassed ?? false) ? "通过，感谢你为CnGal资料站做出的贡献" : "驳回") + (string.IsNullOrWhiteSpace(examine.Comments) ? "" : "，批注『" + examine.Comments + "』"),
                            Link = "peripheries/index/" + periphery.Id,
                            LinkTitle = periphery.Name,
                            Type = (examine.IsPassed ?? false) ? MessageType.ExaminePassed : MessageType.ExamineUnPassed,
                            ApplicationUserId = user.Id
                        });
                    }
                }
            }
            else if (disambig != null)
            {
                if (examine.PrepositionExamineId == null || examine.PrepositionExamineId <= 0)
                {
                    //查找是否有以此为前置审核的审核 如果有 则代表第一此创建
                    if (await _examineRepository.GetAll().AnyAsync(s => s.IsPassed == null && s.PrepositionExamineId == examine.Id))
                    {
                        await _messageRepository.InsertAsync(new Message
                        {
                            Title = (examine.IsPassed ?? false) ? "消歧义页面创建成功提醒" : "消歧义页面创建驳回提醒",
                            PostTime = DateTime.Now.ToCstTime(),
                            Image = "default/logo.png",
                            Rank = "系统",
                            Text = "你创建的消歧义页面『" + (disambig.Name ?? $"Id:{disambig.Id}") + "』被管理员『" + userAdmin.UserName + "』" + ((examine.IsPassed ?? false) ? "审核通过，已经可以被浏览，感谢你为CnGal资料站做出的贡献" : "驳回") + (string.IsNullOrWhiteSpace(examine.Comments) ? "" : "，批注『" + examine.Comments + "』"),
                            Link = "disambigs/index/" + disambig.Id,
                            LinkTitle = disambig.Name,
                            Type = (examine.IsPassed ?? false) ? MessageType.ExaminePassed : MessageType.ExamineUnPassed,
                            ApplicationUserId = user.Id
                        });
                    }
                    else
                    {
                        await _messageRepository.InsertAsync(new Message
                        {
                            Title = (examine.IsPassed ?? false) ? "编辑通过提醒" : "编辑驳回提醒",
                            PostTime = DateTime.Now.ToCstTime(),
                            Image = "default/logo.png",
                            Rank = "系统",
                            Text = "你对消歧义页面『" + (disambig.Name ?? $"Id:{disambig.Id}") + "』的『" + examine.Operation.GetDisplayName() + "』操作被管理员『" + userAdmin.UserName + "』" + ((examine.IsPassed ?? false) ? "通过，感谢你为CnGal资料站做出的贡献" : "驳回") + (string.IsNullOrWhiteSpace(examine.Comments) ? "" : "，批注『" + examine.Comments + "』"),
                            Link = "disambigs/index/" + disambig.Id,
                            LinkTitle = examine.Entry.Name,
                            Type = (examine.IsPassed ?? false) ? MessageType.ExaminePassed : MessageType.ExamineUnPassed,
                            ApplicationUserId = user.Id
                        });
                    }
                }
            }
            else if (tag != null)
            {
                if (examine.PrepositionExamineId == null || examine.PrepositionExamineId <= 0)
                {
                    //查找是否有以此为前置审核的审核 如果有 则代表第一此创建
                    if (await _examineRepository.GetAll().AnyAsync(s => s.IsPassed == null && s.PrepositionExamineId == examine.Id) )
                    {
                        await _messageRepository.InsertAsync(new Message
                        {
                            Title = (examine.IsPassed ?? false) ? "标签创建成功提醒" : "标签创建驳回提醒",
                            PostTime = DateTime.Now.ToCstTime(),
                            Image = "default/logo.png",
                            Rank = "系统",
                            Text = "你创建的标签『" + (tag.Name ?? $"Id:{tag.Id}") + "』被管理员『" + userAdmin.UserName + "』" + ((examine.IsPassed ?? false) ? "审核通过，已经可以被浏览，感谢你为CnGal资料站做出的贡献" : "驳回") + (string.IsNullOrWhiteSpace(examine.Comments) ? "" : "，批注『" + examine.Comments + "』"),
                            Link = "tags/index/" + tag.Id,
                            LinkTitle = tag.Name,
                            Type = (examine.IsPassed ?? false) ? MessageType.ExaminePassed : MessageType.ExamineUnPassed,
                            ApplicationUserId = user.Id
                        });
                    }
                    else
                    {
                        await _messageRepository.InsertAsync(new Message
                        {
                            Title = (examine.IsPassed ?? false) ? "编辑通过提醒" : "编辑驳回提醒",
                            PostTime = DateTime.Now.ToCstTime(),
                            Image = "default/logo.png",
                            Rank = "系统",
                            Text = "你对标签『" + (tag.Name ?? $"Id:{tag.Id}") + "』的『" + examine.Operation.GetDisplayName() + "』操作被管理员『" + userAdmin.UserName + "』" + ((examine.IsPassed ?? false) ? "通过，感谢你为CnGal资料站做出的贡献" : "驳回") + (string.IsNullOrWhiteSpace(examine.Comments) ? "" : "，批注『" + examine.Comments + "』"),
                            Link = "tags/index/" + tag.Id,
                            LinkTitle = tag.Name,
                            Type = (examine.IsPassed ?? false) ? MessageType.ExaminePassed : MessageType.ExamineUnPassed,
                            ApplicationUserId = user.Id
                        });
                    }
                }
            }
            else if (comment != null)
            {
                await _messageRepository.InsertAsync(new Message
                {
                    Title = (examine.IsPassed ?? false) ? "评论审核通过提醒" : "评论驳回提醒",
                    PostTime = DateTime.Now.ToCstTime(),
                    Image = "default/logo.png",
                    Rank = "系统",
                    Text = "你的评论被管理员『" + userAdmin.UserName + "』" + ((examine.IsPassed ?? false) ? "通过，感谢你为CnGal资料站做出的贡献" : "驳回") + (string.IsNullOrWhiteSpace(examine.Comments) ? "" : "，批注『" + examine.Comments + "』"),
                    Link = "",
                    LinkTitle = "",
                    Type = (examine.IsPassed ?? false) ? MessageType.ExaminePassed : MessageType.ExamineUnPassed,
                    ApplicationUserId = user.Id
                });
            }
            else if (playedGame != null)
            {
                await _messageRepository.InsertAsync(new Message
                {
                    Title = (examine.IsPassed ?? false) ? "游玩记录审核通过提醒" : "游玩记录驳回提醒",
                    PostTime = DateTime.Now.ToCstTime(),
                    Image = "default/logo.png",
                    Rank = "系统",
                    Text = "你的游玩记录被管理员『" + userAdmin.UserName + "』" + ((examine.IsPassed ?? false) ? "通过，感谢你为CnGal资料站做出的贡献" : "驳回") + (string.IsNullOrWhiteSpace(examine.Comments) ? "" : "，批注『" + examine.Comments + "』"),
                    Link = "entries/index/" + playedGame.EntryId,
                    LinkTitle = playedGame.Entry?.Name,
                    Type = (examine.IsPassed ?? false) ? MessageType.ExaminePassed : MessageType.ExamineUnPassed,
                    ApplicationUserId = user.Id
                });
            }
            else
            {
                await _messageRepository.InsertAsync(new Message
                {
                    Title = (examine.IsPassed ?? false) ? "编辑通过提醒" : "编辑驳回提醒",
                    PostTime = DateTime.Now.ToCstTime(),
                    Image = "default/logo.png",
                    Rank = "系统",
                    Text = "你的『" + examine.Operation.GetDisplayName() + "』操作被管理员『" + userAdmin.UserName + "』" + ((examine.IsPassed ?? false) ? "通过，感谢你为CnGal资料站做出的贡献" : "驳回") + (string.IsNullOrWhiteSpace(examine.Comments) ? "" : "，批注『" + examine.Comments + "』"),
                    Link = "home/examined/" + examine.Id,
                    LinkTitle = "第" + examine.Id + "条审核记录",
                    Type = (examine.IsPassed ?? false) ? MessageType.ExaminePassed : MessageType.ExamineUnPassed,
                    ApplicationUserId = user.Id
                });

            }

            return new Result { Successful = true };
        }

        /// <summary>
        /// 获取编辑记录概览
        /// </summary>
        /// <param name="id">要对比的编辑记录</param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<ExaminesOverviewViewModel>> GetExaminesOverview(long id)
        {
            var examine = await _examineRepository.GetAll().AsNoTracking().FirstOrDefaultAsync(s => s.Id == id && s.IsPassed == true);
            if (examine == null)
            {
                return NotFound("无法找到该审核");
            }

            var model = new ExaminesOverviewViewModel();
            var examines = new List<Examine>();
            if (examine.EntryId != null)
            {
                var entry = await _entryRepository.GetAll().AsNoTracking().FirstOrDefaultAsync(s => s.Id == examine.EntryId);
                if (entry == null)
                {
                    return NotFound("无法找到审核记录对应的词条");
                }

                model.ObjectId = entry.Id;
                model.ObjectName = entry.DisplayName;
                model.ObjectBriefIntroduction = entry.BriefIntroduction;
                model.Image = (entry.Type == EntryType.Game || entry.Type == EntryType.ProductionGroup) ? _appHelper.GetImagePath(entry.MainPicture, "app.png") : _appHelper.GetImagePath(entry.Thumbnail, "user.png");
                model.IsThumbnail = (entry.Type == EntryType.Game || entry.Type == EntryType.ProductionGroup) ? false : true;
                model.Type = ExaminedNormalListModelType.Entry;

                examines = await _examineRepository.GetAll().AsNoTracking()
                   .Include(s => s.ApplicationUser)
                   .Include(s => s.Entry)
                   .Where(s => s.EntryId == entry.Id && s.IsPassed == true).OrderBy(s => s.Id).ToListAsync();
            }
            else if (examine.ArticleId != null)
            {
                var article = await _articleRepository.GetAll().AsNoTracking().FirstOrDefaultAsync(s => s.Id == examine.ArticleId);
                if (article == null)
                {
                    return NotFound("无法找到审核记录对应的文章");
                }

                model.ObjectId = article.Id;
                model.ObjectName = article.DisplayName;
                model.ObjectBriefIntroduction = article.BriefIntroduction;
                model.Image = _appHelper.GetImagePath(article.MainPicture, "app.png");
                model.IsThumbnail = false;
                model.Type = ExaminedNormalListModelType.Article;

                examines = await _examineRepository.GetAll().AsNoTracking()
                    .Include(s => s.ApplicationUser)
                    .Include(s => s.Article)
                    .Where(s => s.ArticleId == article.Id && s.IsPassed == true).OrderBy(s => s.Id).ToListAsync();
            }
            else if (examine.PeripheryId != null)
            {

                var periphery = await _peripheryRepository.GetAll().AsNoTracking().FirstOrDefaultAsync(s => s.Id == examine.PeripheryId);
                if (periphery == null)
                {
                    return NotFound("无法找到审核记录对应的周边");
                }

                model.ObjectId = periphery.Id;
                model.ObjectName = periphery.DisplayName;
                model.ObjectBriefIntroduction = periphery.BriefIntroduction;
                model.Image = _appHelper.GetImagePath(periphery.MainPicture, "app.png");
                model.IsThumbnail = false;
                model.Type = ExaminedNormalListModelType.Periphery;

                examines = await _examineRepository.GetAll().AsNoTracking()
                   .Include(s => s.ApplicationUser)
                   .Include(s => s.Periphery)
                   .Where(s => s.PeripheryId == periphery.Id && s.IsPassed == true).OrderBy(s => s.Id).ToListAsync();
            }
            else if (examine.TagId != null)
            {

                var tag = await _tagRepository.GetAll().AsNoTracking().FirstOrDefaultAsync(s => s.Id == examine.TagId);
                if (tag == null)
                {
                    return NotFound("无法找到审核记录对应的标签");
                }

                model.ObjectId = tag.Id;
                model.ObjectName = tag.Name;
                model.ObjectBriefIntroduction = tag.BriefIntroduction;
                model.Image = _appHelper.GetImagePath(tag.MainPicture, "app.png");
                model.IsThumbnail = false;
                model.Type = ExaminedNormalListModelType.Tag;

                examines = await _examineRepository.GetAll().AsNoTracking()
                    .Include(s => s.ApplicationUser)
                    .Include(s => s.Tag)
                    .Where(s => s.TagId == tag.Id && s.IsPassed == true).OrderBy(s => s.Id).ToListAsync();
            }
            else
            {
                return BadRequest("无效的类型");
            }



            var examinedView = new ExamineViewModel();

            foreach (var item in examines)
            {
                if (await _examineService.GetExamineView(examinedView, item))
                {
                    model.Examines.Add(new EditRecordAloneViewModel
                    {
                        ApplyTime = item.ApplyTime,
                        Comments = item.Comments,
                        EditOverview = examinedView.EditOverview,
                        Id = item.Id,
                        Note = item.Note,
                        Operation = item.Operation,
                        PassedAdminName = item.PassedAdminName,
                        PassedTime = item.PassedTime.Value,
                        UserId = item.ApplicationUserId,
                        IsSelected = item.Id == id,
                        UserName = item.ApplicationUser.UserName
                    });
                }

            }

            return model;
        }
    }
}
