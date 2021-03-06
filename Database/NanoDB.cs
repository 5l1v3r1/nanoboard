using System;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace nboard
{
    class NanoDB
    {
        static Random random = new Random();
        private const string Index = "index.db";
        private const string Data = "data.db";
        private const string HideList = "hide.list";
        //private const string Bookmarks = "bookmarks.list";

        private readonly Dictionary<Hash, NanoPost> _posts;
        private readonly List<NanoPost> _addedPosts;
        private readonly HashSet<Hash> _threads;
        private readonly Dictionary<Hash, List<NanoPost>> _threadPosts;
        private readonly HashSet<NanoPost> _new;
        private HashSet<string> _hideList;
        private readonly HashSet<string> _onceList;
        private readonly HashSet<string> _bookmarks;

        [Obsolete]
        public Hash RootHash { get; private set; }

        public const string CategoriesHashValue = "bdd4b5fc1b3a933367bc6830fef72a35";
        public const string RootHashValue = "f682830a470200d738d32c69e6c2b8a4";

        public event Action<NanoPost> Updated = delegate(NanoPost obj) {};
        //public event Action<NanoPost> BookmarkAdded = delegate(NanoPost obj) {};

        public void Init()
        {
            var root = new NanoPost(Hash.CreateZero(), NanoPost.RootStub);
            AddPost(root, false);
            RootHash = root.GetHash();

            var cat = new NanoPost(RootHash, "[b]КАТЕГОРИИ[/b]\nЧтобы создать новую категорию, ответьте на это сообщение.\nОтветьте на одну из категорий, чтобы создать там тред.");
            AddPost(cat, false);
            var cathash = cat.GetHash();
            AddPost(new NanoPost(cathash, "[b]Автомобили/Мотоциклы[/b]"), false);
            AddPost(new NanoPost(cathash, "[b]Бред/Разное[/b]"), false);
            AddPost(new NanoPost(cathash, "[b]Видеоигры[/b]"), false);
            AddPost(new NanoPost(cathash, "[b]Выживание[/b]"), false);
            AddPost(new NanoPost(cathash, "[b]Железо/Софт[/b]"), false);
            AddPost(new NanoPost(cathash, "[b]Иностранные языки[/b]"), false);
            AddPost(new NanoPost(cathash, "[b]Кино и ТВ[/b]"), false);
            AddPost(new NanoPost(cathash, "[b]Книги[/b]"), false);
            AddPost(new NanoPost(cathash, "[b]Криптоанархия[/b]"), false);
            AddPost(new NanoPost(cathash, "[b]Музыка[/b]"), false);
            AddPost(new NanoPost(cathash, "[b]Мода и стиль[/b]"), false);
            AddPost(new NanoPost(cathash, "[b]Наука[/b]"), false);
            AddPost(new NanoPost(cathash, "[b]Обсуждение Наноборды[/b]"), false);
            AddPost(new NanoPost(cathash, "[b]Паранормальное[/b]"), false);
            AddPost(new NanoPost(cathash, "[b]Политика[/b]"), false);
            AddPost(new NanoPost(cathash, "[b]Психология[/b]"), false);
            AddPost(new NanoPost(cathash, "[b]Программирование[/b]"), false);
            AddPost(new NanoPost(cathash, "[b]Реквесты[/b]"), false);
            AddPost(new NanoPost(cathash, "[b]Смартфоны/Планшеты[/b]"), false);
            AddPost(new NanoPost(cathash, "[b]Секс[/b]"), false);
            AddPost(new NanoPost(cathash, "[b]Спорт[/b]"), false);
            AddPost(new NanoPost(cathash, "[b]Творчество[/b]"), false);
            AddPost(new NanoPost(cathash, "[b]Японская культура[/b]"), false);
            AddPost(new NanoPost(cathash, "[b]18+[/b]"), false);

            foreach (var p in _posts)
            {
                p.Value.NumberTag = int.MaxValue;
            }

            try
            {
                if (File.Exists(HideList))
                {
                    _hideList = new HashSet<string>(File.ReadAllLines(HideList));
                }
            }
            catch (Exception e)
            {
                Logger.LogError("Error wile reading hide.list\n" + e.ToString());
            }
        }

        public NanoDB()
        {
            _posts = new Dictionary<Hash, NanoPost>();
            _addedPosts = new List<NanoPost>();
            _threads = new HashSet<Hash>();
            _new = new HashSet<NanoPost>();
            _threadPosts = new Dictionary<Hash, List<NanoPost>>();
            _hideList = new HashSet<string>();
            _onceList = new HashSet<string>();
            _bookmarks = new HashSet<string>();

            Init();

            /*if (File.Exists(Bookmarks))
            {
                _bookmarks = new HashSet<string>(File.ReadAllLines(Bookmarks));
            }*/

            //AddToBookmarks(RootHash);
        }

        public Hash[] Threads
        {
            get
            {
                return _threads.ToArray();
            }
        }

        public bool IsHiddenListEmpty()
        {
            return _hideList.Count == 0;
        }

        public bool IsHidden(Hash hash)
        {
            return _hideList.Contains(hash.Value) || 
                   _onceList.Contains(hash.Value);
       }

        public void HideOnce(Hash hash)
        {
            if (_onceList.Contains(hash.Value)) return;
            if (hash.Zero) return;
            if (hash.Value == RootHash.Value) return;
            _onceList.Add(hash.Value);
        }

        public void Hide(Hash hash)
        {
            if (_hideList.Contains(hash.Value)) return;
            if (hash.Zero) return;
            if (hash.Value == RootHash.Value) return;
            _hideList.Add(hash.Value);
        }

        public void Unhide(Hash hash)
        {
            if (!_hideList.Contains(hash.Value))
                return;
            if (hash.Zero)
                return;
            if (hash.Value == RootHash.Value)
                return;
            _hideList.Remove(hash.Value);

            if (_onceList.Contains(hash.Value))
            {
                _onceList.Remove(hash.Value);
            }
        }

        /*
        public void AddToBookmarks(Hash hash)
        {
            if (_bookmarks.Contains(hash.Value)) return;
            _bookmarks.Add(hash.Value);
            File.AppendAllText(Bookmarks, hash.Value + "\n");
            BookmarkAdded(Get(hash));
        }

        public void PostRemoveFromBookmarks(Hash hash)
        {
            if (!_bookmarks.Contains(hash.Value))
                return;
            _bookmarks.Remove(hash.Value);
            File.Delete(Bookmarks);
            foreach (var b in _bookmarks)
            {
                File.AppendAllText(Bookmarks, b + "\n");
            }
        }
        */

        public NanoPost[] Bookmarked
        {
            get
            {
                return _bookmarks.Select(b => Get(new Hash(b))).ToArray();
            }
        }

        public NanoPost Get(Hash hash)
        {
            if (!_posts.ContainsKey(hash))
            {
                return null;
            }

            return _posts[hash];
        }

        public int CountAnswers(Hash thread)
        {
            return CountAnswersRecursive(thread);
            //return _threadPosts.ContainsKey(thread) ? _threadPosts[thread].ToArray().ExceptHidden(this).Length : 0;
        }

        public int CountAnswersRecursive(Hash thread)
        {
            if (!_threadPosts.ContainsKey(thread))
                return 0;

            int res = 0;
            var stack = new Stack<Hash>();
            stack.Push(thread);

            while (stack.Count > 0)
            {
                var v = stack.Pop();

                if (_threadPosts.ContainsKey(v))
                {
                    foreach (var p in _threadPosts[v])
                    {
                        res += 1;
                        stack.Push(p.GetHash());
                    }
                }
            }

            return res;
        }

        public NanoPost[] GetExpandedThreadPosts(Hash thread, int depth = 0, List<NanoPost> list = null)
        {
            if (list == null)
            {
                list = new List<NanoPost>();
            }

            if (depth == 0)
            {
                // clear depth
                foreach (var p in _posts)
                {
                    p.Value.DepthTag = 0;
                }
            }


            if (!_threadPosts.ContainsKey(thread))
            {
                if (_posts.ContainsKey(thread))
                {
                    _posts[thread].DepthTag = depth;
                    list.Add(_posts[thread]);
                    return new NanoPost[] { _posts[thread] };
                }
                else
                {
                    return new NanoPost[0];
                }
            }


            if (depth == 0 && _posts.ContainsKey(thread))
            {
                _posts[thread].DepthTag = depth;
                list.Add(_posts[thread]);
            }

            var tps = _threadPosts[thread].OrderBy(p => p.NumberTag).ToArray();

            foreach (var tp in tps)
            {
                tp.DepthTag = depth + 1;
                list.Add(tp);
                GetExpandedThreadPosts(tp.GetHash(), depth + 1, list);
            }

            return list.Distinct().ToArray();
        }

        public NanoPost[] GetThreadPosts(Hash thread, bool eraseDepth = true)
        {
            if (!_threadPosts.ContainsKey(thread))
            {
                if (_posts.ContainsKey(thread))
                {
                    return new NanoPost[] { _posts[thread] };
                }

                else
                {
                    return new NanoPost[0];
                }
            }

            var list = new List<NanoPost>();

            if (_posts.ContainsKey(thread))
            {
                list.Add(_posts[thread]);
            }

            list.AddRange(_threadPosts[thread].ToArray().Sorted());
            if (eraseDepth)
                list.ForEach(p => p.DepthTag = 0);
            return list.ToArray();
        }

        public NanoPost[] GetNewPosts()
        {
            return _new.ToArray();
        }

        public int GetPostCount()
        {
            return _addedPosts.Count;
        }

        public NanoPost GetPost(int index)
        {
            return _addedPosts[index];
        }

        public NanoPost[] GetNLastPosts(int count)
        {
            if (count > _addedPosts.Count)
            {
                return _addedPosts.ToArray();
            }

            return _addedPosts.GetRange(_addedPosts.Count - count, count).ToArray();
        }

        public NanoPost[] GetNRandomPosts(int count)
        {
            List<NanoPost> posts = new List<NanoPost>();

            for (int i = 0; i < count; i++)
            {
                posts.Add(_addedPosts[random.Next(_addedPosts.Count-1)]);
            }

            return posts.ToArray();
        }

        static int _postNo = 0;

        public bool AddPost(NanoPost post)
        {
            if (post.Invalid) return false;
            return AddPost(post, true);
        }

        private bool AddPost(NanoPost post, bool isNew)
        {
            if (_posts.ContainsKey(post.GetHash()))
            {
                return false;
            }

            if (IsHidden(post.GetHash()))
            {
                return false;
            }

            if (post.ReplyTo.Value == CategoriesHashValue)
            {
                post.NumberTag = int.MaxValue;
            }

            else if (post.NumberTag != int.MaxValue)
            {
                post.NumberTag = ++_postNo;
            }

            if (isNew)
            {
                _new.Add(post);
            }

            _addedPosts.Add(post);
            _posts[post.GetHash()] = post;
            _threads.Add(post.ReplyTo);

            if (!_threadPosts.ContainsKey(post.ReplyTo))
            {
                _threadPosts[post.ReplyTo] = new List<NanoPost>();
            }

            _threadPosts[post.ReplyTo].Add(post);

            if (post.ReplyTo.Zero || post.ReplyTo.Value == RootHash.Value)
            {
                _threads.Add(post.GetHash());
            }

            if (isNew)
            {
                Updated(post);
            }

            return true;
        }

        public void RewriteDbExceptHidden()
        {
            try
            {
                int offset = 0;

                if (File.Exists(Data))
                {
                    File.Copy(Data, "data.bak");
                    File.Delete(Data);
                }

                if (File.Exists(Index))
                {
                    File.Copy(Index, "index.bak");
                    File.Delete(Index);
                }

                var all = _posts.Values.ToArray();

                // recursively hide posts
                foreach (var p in all)
                {
                    if (IsHidden(p.GetHash()))
                    {
                        var children = GetExpandedThreadPosts(p.GetHash());

                        foreach (var child in children)
                        {
                            Hide(child.GetHash());
                        }
                    }
                }

                foreach (var p in all)
                {
                    if (IsHidden(p.GetHash()))
                    {
                        continue;
                    }

                    var @string = p.SerializedString();
                    FileUtils.AppendAllBytes(Index, Encoding.UTF8.GetBytes(offset.ToString("x8")));
                    FileUtils.AppendAllBytes(Index, Encoding.UTF8.GetBytes(@string.Length.ToString("x8")));
                    FileUtils.AppendAllBytes(Data, p.SerializedBytes());
                    offset += @string.Length;
                }

                if (File.Exists(HideList))
                {
                    File.Delete(HideList);
                }

                File.AppendAllLines(HideList, _hideList);
                File.Delete("data.bak");
                File.Delete("index.bak");
            }

            catch (Exception e)
            {
                Logger.LogError("Can't rewrite db:\n", e.ToString());
            }
        }

        public void WriteNewPosts(bool clear = true)
        {
            try
            {
                int offset = 0;

                if (File.Exists(Data))
                {
                    string posts = Encoding.UTF8.GetString(File.ReadAllBytes(Data));
                    offset = posts.Length;
                }

                foreach (var p in _new)
                {
                    var @string = p.SerializedString();
                    FileUtils.AppendAllBytes(Index, Encoding.UTF8.GetBytes(offset.ToString("x8")));
                    FileUtils.AppendAllBytes(Index, Encoding.UTF8.GetBytes(@string.Length.ToString("x8")));
                    FileUtils.AppendAllBytes(Data, p.SerializedBytes());
                    offset += @string.Length;
                }

                if (clear)
                {
                    _new.Clear();
                }
            }
            catch (Exception e)
            {
                Logger.LogError("Error updating db:\n", e.ToString());
            }
        }

        public void ClearDb()
        {
            _posts.Clear();
            _addedPosts.Clear();
            _threads.Clear();
            _threadPosts.Clear();
            _new.Clear();
            _hideList.Clear();
            _onceList.Clear();
            _postNo = 0;
            Init();
        }

        public void ReadPosts()
        {
            if (!File.Exists(Index) || !File.Exists(Data))
                return;

            string indexes = Encoding.UTF8.GetString(File.ReadAllBytes(Index));
            string posts = Encoding.UTF8.GetString(File.ReadAllBytes(Data));

            try
            {

                for (int i = 0; i < indexes.Length / 8; i += 2)
                {
                    string offset = indexes.Substring(i * 8, 8);
                    string length = indexes.Substring(i * 8 + 8, 8);
                    string rawpost = posts.Substring(
                                     int.Parse(offset, System.Globalization.NumberStyles.HexNumber), 
                                     int.Parse(length, System.Globalization.NumberStyles.HexNumber));
                    var post = new NanoPost(rawpost);

                    if (!SpamDetector.IsSpam(post.Message))
                    {
                        AddPost(new NanoPost(rawpost), false);
                    }
                }
            }

            catch(Exception e)
            {
                Logger.LogError("Error while reading posts db:\n" + e.ToString());
            }
        }

        public void CropDbFiles(int postsToLeft)
        {
            throw new NotImplementedException();
        }

        public void DeleteDbFiles()
        {
            if (!File.Exists(Index) || !File.Exists(Data)) return;
        
            File.Delete(Index);
            File.Delete(Data);
        }
    }
}