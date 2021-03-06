﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using BLL.Tools;
using DbModel.Location.AreaAndDev;
using DbModel.Location.Data;
using DbModel.Location.Person;
using DbModel.Location.Relation;
using DbModel.LocationHistory.Data;
using DbModel.Tools;
using ExcelLib;
using Location.BLL.Tool;
using Location.TModel.Tools;
using System.Threading;
using BLL.Blls.Location;
using BLL.Blls.LocationHistory;
using BLL.Initializers;
using DbModel.Location.Authorizations;
using DbModel.Location.Work;
using DbModel.Tools.InitInfos;

namespace BLL
{
    /// <summary>
    /// 数据库初始化类
    /// </summary>
    public class DbInitializer
    {

        private static bool isInited = false;

        private readonly Bll _bll;

        private CardRoleBll CardRoles => _bll.CardRoles;

        private LocationCardBll LocationCards => _bll.LocationCards;

        private LocationAlarmBll LocationAlarms => _bll.LocationAlarms;

        private LocationCardPositionBll LocationCardPositions => _bll.LocationCardPositions;

        private PositionBll Positions => _bll.Positions;

        private AreaBll Areas => _bll.Areas;

        private AreaAuthorizationBll AreaAuthorizations => _bll.AreaAuthorizations;

        private AreaAuthorizationRecordBll AreaAuthorizationRecords => _bll.AreaAuthorizationRecords;

        private DevModelBll DevModels => _bll.DevModels;

        private DevTypeBll DevTypes => _bll.DevTypes;

        private ConfigArgBll ConfigArgs => _bll.ConfigArgs;

        private PersonnelBll Personnels => _bll.Personnels;

        private DepartmentBll Departments => _bll.Departments;

        private PostBll Posts => _bll.Posts;

        private LocationCardToPersonnelBll LocationCardToPersonnels => _bll.LocationCardToPersonnels;

        public DbInitializer(Bll bll)
        {
            _bll = bll;
        }

        int maxPersonCount = 20;//初始人的数量
        int maxTagCount = 100;//初始卡的数量

        public void InitDbData(int mode, bool isForce = false)
        {
            if (isForce == false && isInited)
            {
                Log.Info("IsInited");
                return;
            }
            isInited = true;

            Log.InfoStart("InitDbData", true);
            if (!_bll.HasData())
            {
                Log.InfoStart("初始化数据库", true);
                if (mode == 0)
                {
                    InitByEntitys();
                }
                else if (mode == 1)
                {
                    InitBySqlFile();
                }
                else
                {
                    Log.Error("InitDbData mode:" + mode);
                }
                Log.InfoEnd("初始化数据库");
            }
            Log.InfoEnd("InitDbData");
        }

        private void InitBySqlFile()
        {
            string initFile = AppDomain.CurrentDomain.BaseDirectory + "Data\\Location.sql";
            if (File.Exists(initFile))
            {
                Log.Info("从sql文件读取");
                try
                {
                    string txt = File.ReadAllText(initFile);
                    int count = _bll.Db.Database.ExecuteNonQuery(txt);
                }
                catch (Exception ex)
                {
                    Log.Error("执行sql语句失败", ex);
                }
            }
            else
            {
                Log.Info("sql文件不存在:" + initFile);

                InitByEntitys();
            }
        }

        public void InitByEntitys()
        {
            InitKKSCode();

            InitTagPositions(true);
            //卡角色、定位卡
            InitDepartments();
            //机构、人员

            InitUsers();
            //登录人员

            new AreaTreeInitializer(_bll).InitAreaAndDev();
            //区域、设备

            InitRealTimePositions();//初始化定位卡初始实时位置

            InitConfigArgs();
            //配置信息
            InitDevModelAndType();
            InitAuthorization();

            //InitArchorSettings();
        }

        public void InitCardAndPerson()
        {
            InitTagPositions(true);
            //卡角色、定位卡
            InitDepartments();
            //机构、人员

            InitUsers();

            InitRealTimePositions();//初始化定位卡初始实时位置
        }

        private void AddPerson(string name, Sexs sex, LocationCard tag, Department dep, Post pst, int worknumber, string phone)
        {
            Personnel person = new Personnel()
            {
                Name = name,
                Sex = sex,
                Enabled = true,
                ParentId = dep.Id,
                WorkNumber = worknumber,
                Phone = phone,
                Pst = pst.Name
            };
            Personnels.Add(person);


            _bll.BindCardToPerson(person, tag);
        }

        Random r=new Random(DateTime.Now.Millisecond);

        public void InitDepartments()
        {
            Log.InfoStart("InitDepartments");

            LocationCardToPersonnels.Clear();
            Personnels.Clear();
            Departments.Clear();

            Department dep0 = new Department() { Name = "根节点", ShowOrder = 0, Parent = null, Type = DepartType.本厂 };
            Departments.Add(dep0);
            Department dep11 = new Department() { Name = "四会电厂", ShowOrder = 0, Parent = dep0, Type = DepartType.本厂 };
            Departments.Add(dep11);
            Department dep12 = new Department() { Name = "维修部门", ShowOrder = 0, Parent = dep11, Type = DepartType.本厂 };
            Departments.Add(dep12);//单个添加可以只是设置Parent
            Department dep13 = new Department() { Name = "发电部门", ShowOrder = 1, ParentId = dep11.Id, Type = DepartType.本厂 };//批量添加必须设置ParentId
            Department dep14 = new Department() { Name = "外委人员", ShowOrder = 2, ParentId = dep11.Id, Type = DepartType.本厂 };
            Department dep15 = new Department() { Name = "访客", ShowOrder = 0, ParentId = dep11.Id, Type = DepartType.本厂 };

            List<Department> subDeps = new List<Department>() { dep12,dep13, dep14,dep15};
            List<Department> subDeps2 = new List<Department>() { dep13, dep14, dep15 };
            Departments.AddRange(subDeps2);

            //Departments.AddRange(dep11, dep12, dep13, dep14, dep15);

            Posts.Clear();
            Post post1 = new Post() { Name = "前台" };
            Post post2 = new Post() { Name = "电工" };
            Post post3 = new Post() { Name = "维修工" };
            Post post4 = new Post() { Name = "保安" };
            Post post5 = new Post() { Name = "经理" };
            Post post6 = new Post() { Name = "电工" };
            Post post7 = new Post() { Name = "访客" };
            var posts = new List<Post>() {post1,post2,post3,post4,post5,post6,post7};
            Posts.AddRange(posts);
            List<LocationCard> tagsT = LocationCards.ToList();
            RandomTool rt=new RandomTool();

            for (int i = 0; i < maxPersonCount && i<tagsT.Count; i++)
            {
                var tag = tagsT[i];
                //int n = r.Next(1);
                int n = i % 2;
                var post = posts[r.Next(posts.Count)];
                var dep = subDeps[r.Next(subDeps.Count)];
                if (n == 0)
                {
                    AddPerson(rt.GetWomanName(), Sexs.女, tag, dep, post, i, rt.GetRandomTel());
                }
                else
                {
                    AddPerson(rt.GetManName(), Sexs.男, tag, dep, post, i, rt.GetRandomTel());
                }
            }


        }

        public void InitUsers()
        {
            Log.InfoStart("InitUsers");
            //User user1 = new User() { Name = "管理员", LoginName = "admin", Password = "admin" };
            //User user2 = new User() { Name = "用户1", LoginName = "user1", Password = "user1" };
            //Users.AddRange(new List<Model.User>() { user1, user2 });
            Log.InfoEnd("InitUsers");
        }

        public void InitKKSCode()
        {

            //先导入KKS再初始化其他数据
            Log.Info("导入土建KKS");
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            Log.Info("BaseDirectory:" + basePath);
            //土建
            string filePath = basePath + "Data\\中电四会部件级KKS编码2017.5.24\\土建\\中电四会热电有限责任公司KKS项目-土建系统-B.xls";
            KKSCodeHelper.ImportKKSFromFile<KKSCode>(new FileInfo(filePath));
            //电气
            filePath = basePath + "Data\\中电四会部件级KKS编码2017.5.24\\电气\\中电四会热电有限责任公司KKS项目-电气盘柜系统-B.xls";
            KKSCodeHelper.ImportKKSFromFile<KKSCode>(new FileInfo(filePath));
            filePath = basePath + "Data\\中电四会部件级KKS编码2017.5.24\\电气\\中电四会热电有限责任公司KKS项目-电气一次总表-B.xls";
            KKSCodeHelper.ImportKKSFromFile<KKSCode>(new FileInfo(filePath));
            filePath = basePath + "Data\\中电四会部件级KKS编码2017.5.24\\电气\\中电四会热电有限责任公司KKS项目-火灾报警系统-B.xls";
            KKSCodeHelper.ImportKKSFromFile<KKSCode>(new FileInfo(filePath));
            filePath = basePath + "Data\\中电四会部件级KKS编码2017.5.24\\电气\\中电四会热电有限责任公司KKS项目-视频监控系统-B.xls";
            KKSCodeHelper.ImportKKSFromFile<KKSCode>(new FileInfo(filePath));
            filePath = basePath + "Data\\中电四会部件级KKS编码2017.5.24\\电气\\中电四会热电有限责任公司KKS项目-照明检修箱系统-B.xls";
            KKSCodeHelper.ImportKKSFromFile<KKSCode>(new FileInfo(filePath));
            //锅炉
            filePath = basePath + "Data\\中电四会部件级KKS编码2017.5.24\\锅炉\\中电四会热电有限责任公司KKS项目-#1炉总表-B.xls";
            KKSCodeHelper.ImportKKSFromFile<KKSCode>(new FileInfo(filePath));
            filePath = basePath + "Data\\中电四会部件级KKS编码2017.5.24\\锅炉\\中电四会热电有限责任公司KKS项目-#3炉总表-B.xls";
            KKSCodeHelper.ImportKKSFromFile<KKSCode>(new FileInfo(filePath));
            //化学
            filePath = basePath + "Data\\中电四会部件级KKS编码2017.5.24\\化学\\中电四会热电有限责任公司KKS项目-化学总表-B.xls";
            KKSCodeHelper.ImportKKSFromFile<KKSCode>(new FileInfo(filePath));
            //暖通
            filePath = basePath + "Data\\中电四会部件级KKS编码2017.5.24\\暖通\\中电四会热电有限责任公司KKS项目-暖通系统-B.xls";
            KKSCodeHelper.ImportKKSFromFile<KKSCode>(new FileInfo(filePath));
            //汽机
            filePath = basePath + "Data\\中电四会部件级KKS编码2017.5.24\\汽机\\中电四会热电有限责任公司KKS项目-#1汽机总表-B.xls";
            KKSCodeHelper.ImportKKSFromFile<KKSCode>(new FileInfo(filePath));
            filePath = basePath + "Data\\中电四会部件级KKS编码2017.5.24\\汽机\\中电四会热电有限责任公司KKS项目-#3汽机总表-B.xls";
            KKSCodeHelper.ImportKKSFromFile<KKSCode>(new FileInfo(filePath));
            //燃机
            filePath = basePath + "Data\\中电四会部件级KKS编码2017.5.24\\燃机\\中电四会热电有限责任公司KKS项目-#2燃机总表-B.xls";
            KKSCodeHelper.ImportKKSFromFile<KKSCode>(new FileInfo(filePath));
            filePath = basePath + "Data\\中电四会部件级KKS编码2017.5.24\\燃机\\中电四会热电有限责任公司KKS项目-#4燃机总表-B.xls";
            KKSCodeHelper.ImportKKSFromFile<KKSCode>(new FileInfo(filePath));
            filePath = basePath + "Data\\中电四会部件级KKS编码2017.5.24\\燃机\\中电四会热电有限责任公司KKS项目-调压站系统-B.xls";
            KKSCodeHelper.ImportKKSFromFile<KKSCode>(new FileInfo(filePath));
            //热控
            filePath = basePath + "Data\\中电四会部件级KKS编码2017.5.24\\热控\\中电四会热电有限责任公司KKS项目-热控盘柜系统-B.xls";
            KKSCodeHelper.ImportKKSFromFile<KKSCode>(new FileInfo(filePath));
            //消防
            filePath = basePath + "Data\\中电四会部件级KKS编码2017.5.24\\消防\\中电四会热电有限责任公司KKS项目-消防系统-B.xls";
            KKSCodeHelper.ImportKKSFromFile<KKSCode>(new FileInfo(filePath));

        }

        private CardRoleInitializer iniRole;

        /// <summary>
        /// 初始化实时位置信息
        /// </summary>
        public void InitRealTimePositions()
        {
            DateTime dt = DateTime.Now;
            long TimeStamp = TimeConvert.DateTimeToTimeStamp(dt);

            LocationCardPositions.Clear();
            List<LocationCardPosition> tagpositions = new List<LocationCardPosition>();
            var cardPersons = _bll.LocationCardToPersonnels.ToList();//和卡绑定的人才有位置信息
            var cards = _bll.LocationCards.ToList();
            var area = _bll.Areas.Find(2);//电厂
            var bound = area.InitBound;
            bound.Points = _bll.Points.FindAll(i => i.BoundId == bound.Id);

            for (int i = 0; i < cardPersons.Count; i++)
            {
                var cp = cardPersons[i];
                var card = cards.Find(j => j.Id == cp.LocationCardId);
                if (i < 10)//这部分固定初始位置
                {
                    var tagposition = new LocationCardPosition() { CardId = cp.LocationCardId, Id = card.Code, X = 2292.5f+i, Y = 2, Z = 1715.5f, DateTime = dt, DateTimeStamp = TimeStamp, Power = 0, Number = i, Flag = "0:0:0:0:0", AreaId = 2, PersonId = cp.Id };
                    tagpositions.Add(tagposition);
                }
                else//这部分随机初始位置
                {
                    var x = r.Next((int)bound.GetSizeX()) + bound.MinX;
                    var z = r.Next((int)bound.GetSizeY()) + bound.MinY;
                    while (!bound.Contains(x, z))
                    {
                        x = r.Next((int)bound.GetSizeX()) + bound.MinX;
                        z = r.Next((int)bound.GetSizeY()) + bound.MinY;
                    }
                    var tagposition = new LocationCardPosition() { CardId = cp.LocationCardId, Id = card.Code, X = x, Y = 2, Z = z, DateTime = dt, DateTimeStamp = TimeStamp, Power = 0, Number = i, Flag = "0:0:0:0:0",  PersonId = cp.Id };
                    tagpositions.Add(tagposition);
                }
            }
            LocationCardPositions.AddRange(tagpositions);
        }

        public void InitTagPositions(bool initRoles)
        {
            LocationAlarms.Clear();//清空告警
            LocationCards.Clear();//清空标签卡

            if (initRoles)
            {
                CardRoles.Clear();//清空角色
                iniRole = new CardRoleInitializer(_bll);
                iniRole.InitData();//初始化标签角色
            }
            var roles = GetRoles();

            Random r=new Random(DateTime.Now.Millisecond);
            Log.InfoStart("InitTagPositions");
            List<LocationCard> tags = new List<LocationCard>();
            string startCode = "0906";
            int startNumber = Convert.ToInt32(startCode, 16);
            for (int i = 0; i < maxTagCount; i++)//400张卡
            {

                if (i >= 15)
                {
                    int number = startNumber + i;
                    string code = "0" + Convert.ToString(number, 16).ToUpper();
                    //var role = roles[r.Next(roles.Count)];//随机分配角色
                    //var tag1 = new LocationCard() { Name = code, Code = code, CardRoleId = role.Id };
                    var tag1 = new LocationCard() { Name = code, Code = code };
                    tags.Add(tag1);
                }
                else
                {
                    var role = roles[r.Next(roles.Count)];//随机分配角色
                    var tag1 = new LocationCard() { Name = "标签" + i, Code = "000" + (i + 1), CardRoleId = role.Id };
                    tags.Add(tag1);
                }
            }
            LocationCards.AddRange(tags);

            //DateTime dt = DateTime.Now;
            //long TimeStamp = TimeConvert.DateTimeToTimeStamp(dt);
            //Position position1 = new Position() { Code = "002", X = -50, Y = -50, Z = -50, DateTime = dt, DateTimeStamp = TimeStamp, Power = 0, Number = 0, Flag = "0:0:0:0:0" };
            //Position position2 = new Position() { Code = "002", X = 0, Y = 0, Z = 0, DateTime = dt, DateTimeStamp = TimeStamp, Power = 0, Number = 0, Flag = "0:0:0:0:0" };
            //Position position3 = new Position() { Code = "002", X = 50, Y = 50, Z = 50, DateTime = dt, DateTimeStamp = TimeStamp, Power = 0, Number = 0, Flag = "0:0:0:0:0" };
            //Position position4 = new Position() { Code = "002", X = 100, Y = 100, Z = 100, DateTime = dt, DateTimeStamp = TimeStamp, Power = 0, Number = 0, Flag = "0:0:0:0:1" };
            //Position position5 = new Position() { Code = "002", X = 150, Y = 150, Z = 150, DateTime = dt, DateTimeStamp = TimeStamp, Power = 0, Number = 0, Flag = "0:0:0:0:0" };
            //Position position6 = new Position() { Code = "002", X = 200, Y = 200, Z = 200, DateTime = dt, DateTimeStamp = TimeStamp, Power = 0, Number = 0, Flag = "0:0:0:0:0" };
            //Position position7 = new Position() { Code = "002", X = 250, Y = 250, Z = 250, DateTime = dt, DateTimeStamp = TimeStamp, Power = 0, Number = 0, Flag = "0:0:0:0:0" };
            //Position position8 = new Position() { Code = "002", X = 300, Y = 300, Z = 300, DateTime = dt, DateTimeStamp = TimeStamp, Power = 0, Number = 0, Flag = "0:0:0:0:1" };
            //Position position9 = new Position() { Code = "002", X = 350, Y = 350, Z = 350, DateTime = dt, DateTimeStamp = TimeStamp, Power = 0, Number = 0, Flag = "0:0:0:0:0" };
            //Position position10 = new Position() { Code = "002", X = 400, Y = 400, Z = 400, DateTime = dt, DateTimeStamp = TimeStamp, Power = 0, Number = 0, Flag = "0:0:0:0:0" };
            //Position position11 = new Position() { Code = "002", X = 500, Y = 500, Z = 450, DateTime = dt, DateTimeStamp = TimeStamp, Power = 0, Number = 0, Flag = "0:0:0:0:1" };
            //Position position12 = new Position() { Code = "002", X = 600, Y = 600, Z = 500, DateTime = dt, DateTimeStamp = TimeStamp, Power = 0, Number = 0, Flag = "0:0:0:0:0" };
            //Position position13 = new Position() { Code = "002", X = 700, Y = 700, Z = 550, DateTime = dt, DateTimeStamp = TimeStamp, Power = 0, Number = 0, Flag = "0:0:0:0:0" };
            //Position position14 = new Position() { Code = "002", X = 800, Y = 800, Z = 600, DateTime = dt, DateTimeStamp = TimeStamp, Power = 0, Number = 0, Flag = "0:0:0:0:0" };
            //Position position15 = new Position() { Code = "002", X = 900, Y = 900, Z = 650, DateTime = dt, DateTimeStamp = TimeStamp, Power = 0, Number = 0, Flag = "0:0:0:0:1" };
            //Position position16 = new Position() { Code = "002", X = 1100, Y = 1100, Z = 700, DateTime = dt, DateTimeStamp = TimeStamp, Power = 0, Number = 0, Flag = "0:0:0:0:0" };
            //Position position17 = new Position() { Code = "002", X = 1200, Y = 1200, Z = 750, DateTime = dt, DateTimeStamp = TimeStamp, Power = 0, Number = 0, Flag = "0:0:0:0:0" };
            //Position position18 = new Position() { Code = "002", X = 1300, Y = 1300, Z = 800, DateTime = dt, DateTimeStamp = TimeStamp, Power = 0, Number = 0, Flag = "0:0:0:0:0" };
            //Position position19 = new Position() { Code = "002", X = 1400, Y = 1400, Z = 850, DateTime = dt, DateTimeStamp = TimeStamp, Power = 0, Number = 0, Flag = "0:0:0:0:0" };
            //Position position20 = new Position() { Code = "002", X = 1500, Y = 1500, Z = 900, DateTime = dt, DateTimeStamp = TimeStamp, Power = 0, Number = 0, Flag = "0:0:0:0:0" };
            //List<Position> positions = new List<Position>() { position1, position2, position3, position4, position5, position6, position7, position8, position9, position10, position11, position12, position13, position14, position15, position16, position17, position18, position19, position20 };
            //Positions.Clear();
            //Positions.AddRange(positions);
            Log.InfoEnd("InitTagPositions");
        }

        public List<CardRole> GetRoles()
        {
            List<CardRole> roles = null;
            if (iniRole != null && iniRole.roles != null)
            {
                roles = iniRole.roles;
            }
            else
            {
                roles = _bll.CardRoles.ToList();
            }
            return roles;
        }

        public void InitAuthorization()
        {
            var roles = GetRoles();
            AuthorizationInitializer ai = new AuthorizationInitializer(_bll);
            ai.InitAuthorization(roles);
        }
        public void ClearAuthorization()
        {
            AuthorizationInitializer ai = new AuthorizationInitializer(_bll);
            ai.Clear();
        }

        public void SetAlamArea(int areaId)
        {
            var roles = GetRoles();
            AuthorizationInitializer ai = new AuthorizationInitializer(_bll);
            ai.SetAlarmArea(roles,areaId);
        }

        public void SaveAuthorization()
        {
            AuthorizationInitializer ai = new AuthorizationInitializer(_bll);
            ai.Save();
        }

        public void LoadAuthorization()
        {
            var roles = GetRoles();
            AuthorizationInitializer ai = new AuthorizationInitializer(_bll);
            ai.Load(roles);
        }


        /// <summary>
        /// 初始化设备模型和类型表
        /// </summary>
        public void InitDevModelAndType()
        {
            ////设备模型信息\\DevTypeModel.sql
            //string initFile = AppDomain.CurrentDomain.BaseDirectory + "Data\\设备模型信息\\DevTypeModel.sql";
            //if (File.Exists(initFile))
            //{
            //    try
            //    {
            //        string txt = File.ReadAllText(initFile);
            //        int count = Db.Database.ExecuteNonQuery(txt);
            //    }
            //    catch (Exception ex)
            //    {
            //        Log.Error("执行sql语句失败", ex);
            //    }
            //}
            //else
            //{
            //    Log.Info("sql文件不存在:" + initFile);
            //}
            //因为sqlite不支持改成从表格导入

            var list = DbInfoHelper.GetDevModels();
            DevModels.AddRange(list);

            var list2 = DbInfoHelper.GetDevTypes();
            DevTypes.AddRange(list2);
        }

        public static List<T> LoadExcelToList<T>(string filePath) where T : class, new()
        {
            DataTable dt = ExcelHelper.LoadTable(new FileInfo(filePath), "", true);
            List<T> list = new List<T>();
            Type type = typeof(T);
            for (int j = 0; j < dt.Rows.Count; j++)
            {
                var row = dt.Rows[j];
                T item = new T();
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    var column = dt.Columns[i];
                    var value = row.ItemArray[i];
                    var pt = type.GetProperty(column.ColumnName);
                    if (pt == null) continue;
                    pt.SetValueEx(item, value);
                }
                list.Add(item);
            }
            return list;
        }

        public void InitConfigArgs()
        {
            ConfigArgs.Add(new ConfigArg()
            {
                Key = "TransferOfAxes.Zero",
                Value = "1372.066,0,1013.352",
                ValueType = "Vector3",
                Name = "坐标转换原点",
                Describe = "坐标转换原点（偏移)",
                Classify = "Axes"
            });
            ConfigArgs.Add(new ConfigArg()
            {
                Key = "TransferOfAxes.Scale",
                Value = "1.685735,0,1.686961",
                ValueType = "Vector3",
                Name = "坐标转换比例",
                Describe = "坐标转换比例",
                Classify = "Axes"
            });
            ConfigArgs.Add(new ConfigArg()
            {
                Key = "TransferOfAxes.Direction",
                Value = "-1,1,-1",
                ValueType = "Vector3",
                Name = "坐标转换方向",
                Describe = "坐标转换方向",
                Classify = "Axes"
            });
        }

        public void InitArchorSettings()
        {
            var list2 = DbInfoHelper.GetArchorSettings();
            _bll.ArchorSettings.Clear();
            _bll.ArchorSettings.AddRange(list2);
        }
    }
}
