using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System;
using System.IO;
using LitJson;

public class Mvshlf : EditorWindow {

    public class parseJSON
    {
        public string id;
        public string title;
        public string url;
        public string downloadUrl;
        public string previewUrl;
        public Texture preview;
        public string description;
        public string GUID;
    }

    public class ClipPreview
    {
        public string id;
        public string title;
        public string url;
        public string downloadUrl;
        public string previewUrl;
        public Texture preview;
        public string description;
        public string[] comments;
    }

    public class Comment
    {
        public string author;
        public string comment;
    }

    public static bool test = false;
    string myString = "Martialarts";
    string myApiKey = "";
    string myComment = "";

    List<parseJSON> searchResults = new List<parseJSON>();
    List<Comment> comments = new List<Comment>();
    string activeGUID = "";
    Queue<RequestJob> jobList = new Queue<RequestJob>();
    string MVSHLF_API_KEY = "MVSHLF_API_KEY";
    string baseUrl = "https://moveshelf.com";
    string baseApiUrl = "https://moveshelf.com/graphql";
    double time = 0;
    double MAX_RESULTS = 10;

    ClipPreview previewContainer = new ClipPreview();

    public delegate void RequestHandler(DownloadHandler handler, object[] args);

    UnityWebRequest www;
    static AnimationClip clip;


    [Serializable]
    public class SearchQuery
    {
        [Serializable]
        public class QueryVariables
        {
            public string query;
        }
        public string query;
        public QueryVariables variables;
        public SearchQuery() {
            query = "query search($query: String) { mocapClips(search: $query) { edges { node{ title, id, description, originalDataDownloadUri} } } }";
            variables = new QueryVariables();
        }
        public void setKeyword(string keyword) {
            variables.query = keyword;
        }
        public string getQuery() {
            return EditorJsonUtility.ToJson(this);
        }
    }

    [Serializable]
    public class CreateCommentMutation
    {
        [Serializable]
        public class CommentMutationVariables
        {
            [Serializable]
            public class CommentCreationInput
            {
                public string clipId;
                public string comment;
            }
            public CommentCreationInput commentInputs = new CommentCreationInput();
        }

        public string query;
        public CommentMutationVariables variables;
        public CreateCommentMutation()
        {
            variables = new CommentMutationVariables();
            query = "mutation createComment($commentInputs: CommentCreationInput!) { createComment(inputs: $commentInputs) { comment { id, comment } } }";
        }

        public void setComment(string id, string comment)
        {
            variables.commentInputs.clipId = id;
            variables.commentInputs.comment = comment;
        }
        public string getMutation() {
            return EditorJsonUtility.ToJson(this);
        }
    }

    [Serializable]
    public class MotionClipQuery
    {
        [Serializable]
        public class QueryVariables
        {
            public string id;
        }
        public string query;
        public QueryVariables variables;
        public MotionClipQuery()
        {
            query = "query node($id: ID!) { node(id: $id) { ... on MocapClip { id, title, description, originalDataDownloadUri, comments { edges { node { author { displayName }, comment } } } } } }";
            variables = new QueryVariables();
        }
        public void setId(string id)
        {
            variables.id = id;
        }
        public string getQuery()
        {
            return EditorJsonUtility.ToJson(this);
        }
    }

    public class RequestJob
    {
        public double time;
        public UnityWebRequest www;
        public RequestHandler responseHandler;
        public List<object> actionArgs = new List<object>();

        public RequestJob(UnityWebRequest request, object []args, RequestHandler handler) {
            www = request;
            responseHandler = handler;
            foreach (object arg in args)
                actionArgs.Add(arg);
        }
        public void waitForRequest(double t)
        {
            if (www.isHttpError || www.isNetworkError)
            {
                Debug.Log("error");
            }
            else
            {
                while (www.downloadProgress < 1 && !www.downloadHandler.isDone)
                {
                    EditorUtility.DisplayProgressBar("Fetching data...", "Please wait while your request is completed!", www.downloadProgress / 1);
                };

                EditorUtility.ClearProgressBar();
            }
            time = t;
        }
        public void sendRequest() {
            www.SendWebRequest();
        }
    }


    // Add menu named "My Window" to the Window menu
    [MenuItem("Moveshelf/Search animation")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        Mvshlf window = (Mvshlf)EditorWindow.GetWindow(typeof(Mvshlf));
        window.Show();
    }

    [MenuItem("Moveshelf/moveshelf.com")]
    static void Open()
    {

    }

    void getMotion(DownloadHandler handler, object [] args)
    {
        string target = "Mvshlf";
        if(!AssetDatabase.IsValidFolder("Assets/" + target))
        {
            string guid = AssetDatabase.CreateFolder("Assets", target);
            string newFolderPath = AssetDatabase.GUIDToAssetPath(guid);
        }
  
        byte[] data = handler.data;// www.downloadHandler.data;
        string name = "untitled";

        if (args.Length > 0)
            name = (string)args[0];

        var path = EditorUtility.SaveFilePanel( //SaveFilePanelInProject( //
            "Save motion as fbx",
            target,
            name, "fbx");

        if (path.Length != 0)
        {
            File.WriteAllBytes(path, data);
            if (path.StartsWith(Application.dataPath))
            {
                path = "Assets" + path.Substring(Application.dataPath.Length);
                activeGUID = AssetDatabase.AssetPathToGUID(path);
                AssetDatabase.Refresh();
            }
        }
    }

    void OnEnable()
    {
        //Load API from Environment Variables, if present
        myApiKey = EditorPrefs.GetString(MVSHLF_API_KEY);
    }

    void sendComments(DownloadHandler handler, object[] args)
    {
        string response = handler.text;
        Debug.Log(response);
    }

    void getComments(DownloadHandler handler, object[] args)
    {
        string response = handler.text;
        JsonData jsonData = JsonMapper.ToObject(response);
        JsonData jsonvale = jsonData["data"]["node"]["comments"]["edges"];
        comments.Clear();
        for (int i = 0; i < jsonvale.Count; i++)
        {
            JsonData node = jsonvale[i]["node"];
            Comment com = new Comment();
            com.author = node["author"]["displayName"].ToString();
            com.comment = node["comment"].ToString();
            comments.Add(com);
        }
    }

    void getSearchResults(DownloadHandler downloaHandler, object[] args)
    {
        string response = downloaHandler.text;

        JsonData jsonData = JsonMapper.ToObject(response);
        searchResults.Clear();
        JsonData jsonvale = jsonData["data"]["mocapClips"]["edges"];
        for (int i = 0; i < jsonvale.Count; i++)
        {
            parseJSON parsejson;
            parsejson = new parseJSON();
            JsonData node = jsonvale[i]["node"];
            parsejson.id = node["id"].ToString();
            parsejson.title = node["title"].ToString();
            parsejson.description = node["description"].ToString();
            if(node["originalDataDownloadUri"] != null)
                parsejson.downloadUrl = node["originalDataDownloadUri"].ToString();
            parsejson.url = baseUrl + "/edit/" + node["id"].ToString();
            parsejson.previewUrl = baseUrl + "/preview_image/" + node["id"].ToString();
            searchResults.Add(parsejson);
            if (searchResults.Count > MAX_RESULTS)
                break;
        }
    }

    void EditorUpdate()
    {
        time += 0.01;
        if (jobList.Count > 0)
        {
            if (time - jobList.Peek().time > 0.1)
            {
                RequestJob lastJob = jobList.Dequeue();
                lastJob.responseHandler(lastJob.www.downloadHandler, lastJob.actionArgs.ToArray());
            }
        }
    }

    void getTexture(DownloadHandler handler, object [] args)
    {
        ClipPreview container = (ClipPreview)args[0];
        container.preview = ((DownloadHandlerTexture)handler).texture;
    }

    void downloadTextureFromUrl(string url, object[] args = null)
    {
        RequestJob newJob = new RequestJob(UnityWebRequestTexture.GetTexture(url), args, getTexture);
        newJob.sendRequest();
        newJob.waitForRequest(time);
        jobList.Enqueue(newJob);

    }

    void downloadDataFromUrl(string url, RequestHandler handler, object [] args)
    {
        RequestJob newJob = new RequestJob(UnityWebRequest.Get(url), args, handler);
        newJob.sendRequest();
        newJob.waitForRequest(time);
        jobList.Enqueue(newJob);
    }

    UnityWebRequest getRawJsonResut(string url, string jsonStr, string apiKey) {
        UnityWebRequest www = new UnityWebRequest(url, "POST");

        www = new UnityWebRequest(url, "POST");
        byte[] data = System.Text.Encoding.UTF8.GetBytes(jsonStr);
        www.uploadHandler = (UploadHandlerRaw)new UploadHandlerRaw(data);
        www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

        www.SetRequestHeader("Authorization", apiKey);
        www.SetRequestHeader("Content-Type", "application/json");
        www.chunkedTransfer = false;

        return www;
    }

    void downloadFromUrl(string url, RequestHandler handler, object[] args = null)
    {
        string jsonStr = (string)args[0];
        RequestJob newJob = new RequestJob(getRawJsonResut(url, jsonStr, myApiKey), args, handler);
        newJob.sendRequest();
        newJob.waitForRequest(time);
        jobList.Enqueue(newJob);
    }

    void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        myApiKey = EditorGUILayout.PasswordField("API KEY", myApiKey);
        if (GUILayout.Button("Set API Key", GUILayout.Width(110)))
        {
            EditorPrefs.SetString(MVSHLF_API_KEY, myApiKey);
        }

        EditorGUILayout.EndHorizontal();
        GUILayout.Label("Quick search", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        myString = EditorGUILayout.TextField("Keywords", myString);
        if (GUILayout.Button("SEARCH", GUILayout.Width(110)))
        {
            EditorApplication.update += EditorUpdate;
            SearchQuery query = new SearchQuery();
            query.setKeyword(myString);
            string[] args = { query.getQuery() };
            downloadFromUrl(baseApiUrl, getSearchResults, args);
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Label("Search results", EditorStyles.boldLabel);
        for (int i = 0; i < searchResults.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 340;
            EditorGUILayout.PrefixLabel(searchResults[i].title);
            GUILayout.FlexibleSpace();

            if (searchResults[i].downloadUrl != null && GUILayout.Button("import", GUILayout.Width(80)))
            {
                string[] args = { searchResults[i].title };
                downloadDataFromUrl(searchResults[i].downloadUrl, getMotion, args);
            }
            if (GUILayout.Button("preview", GUILayout.Width(80)))
            {
                ClipPreview[] args = { previewContainer };
                previewContainer.title = searchResults[i].title;
                previewContainer.description = searchResults[i].description;
                previewContainer.url = searchResults[i].url;
                previewContainer.id = searchResults[i].id;
                downloadTextureFromUrl(searchResults[i].previewUrl, args);
            }
            EditorGUILayout.EndHorizontal();
        }

        if (previewContainer.preview)
        {
            GUILayout.Label("Clip info", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("See it on Moveshelf.com", GUILayout.Width(280)))
            {
                Application.OpenURL(previewContainer.url);
            }
            if (GUILayout.Button("Get comments", GUILayout.Width(110)))
            {
                MotionClipQuery query = new MotionClipQuery();
                query.setId(previewContainer.id);
                string[] args = { query.getQuery() };
                downloadFromUrl(baseApiUrl, getComments, args);
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Label("Title: " + previewContainer.title);
            GUILayout.Label("Description: " + previewContainer.description);
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 150;
            myComment = EditorGUILayout.TextField("Comment: ", myComment);
            if (GUILayout.Button("Send", GUILayout.Width(110)))
            {
                CreateCommentMutation mutation = new CreateCommentMutation();
                mutation.setComment(previewContainer.id, myComment);
                string[] args = { mutation.getMutation() };
                downloadFromUrl(baseApiUrl, sendComments, args);
            }

            EditorGUILayout.EndHorizontal();
            for (int i = comments.Count - 1; i >= 0; i--)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(comments[i].author + ", " + comments[i].comment);
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.Label("Preview:");
            EditorGUI.DrawPreviewTexture(new Rect(10, 210 + searchResults.Count*21 + comments.Count * 18, 600, 350), previewContainer.preview);
        }
    }

}


