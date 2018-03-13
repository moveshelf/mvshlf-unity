using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Text;
using LitJson;
using Utils = Moveshelf.Utils;
using GraphQl = Moveshelf.GraphQl;
using Relay = Moveshelf.Relay;

namespace Moveshelf {
    public struct Author {
        public string displayName;
    }

    public struct Comment {
        public Author author;
        public string comment;
    }

    public struct ClipSummary {
        public string id;
        public string title;
    }

    public struct ClipDetail {
        public string id;
        public string title;
        public string description;
        public string previewImageUri;
        public Relay.Connection<Comment> comments;
    }

    public struct ClipDownloadDetails {
        public string originalDataDownloadUri;
        public string originalFileName;
    }

    public struct SearchResult {
        public Relay.Connection<ClipSummary> mocapClips;
    }

    public struct GetDetailResult {
        public ClipDetail node;
    }

    public struct GetDownloadUriResult {
        public ClipDownloadDetails node;
    }

    public struct CreateCommentResult {
        public struct Response {
            public ClipDetail clip;
        }
        public Response createComment;
    }

    public class SearchTool : EditorWindow {
        private const string MVSHLF_API_KEY = "MVSHLF_API_KEY";
        private const int BUTTON_WIDTH = 80;
        private const int LABEL_WIDTH = 80;

        private GraphQl.Client client;
        private SearchResult searchResults;
        private ClipDetail clipDetails;
        private Texture2D clipPreviewImage;
        private Vector2 scrollPos;

        private string myString = "Martialarts";
        private string myApiKey = "";
        private string myComment = "";

        private const string clipDetailFragment =
            @"fragment clipDetailFragment on MocapClip {
                id
                    title
                    description
                    previewImageUri
                    comments(order: ASCENDING, last: 10) {
                        edges {
                            node {
                                author {
                                    displayName
                                }
                                comment
                            }
                        }
                    }
            }";

        private void search(string keyword) {
            const string query =
                @"query search($keyword: String!) {
                    mocapClips(search: $keyword,
                            filter: {fileType: FBX, allowDownload: true}) {
                        edges {
                            node {
                                id
                                    title
                            }
                        }
                    }
                }";
            this.searchResults = client.execute<SearchResult>(query, "keyword", keyword);
        }

        private void getClipDetails(string clipId) {
            const string query =
                @"query clipDetails($id: ID!) {
                    node(id: $id) {
                        ... clipDetailFragment
                    }
                }";
            var res = client.execute<GetDetailResult>(
                    query + clipDetailFragment,
                    "id", clipId);
            updateClipDetails(res.node);
        }

        private void createComment(string clipId, string comment) {
            const string query =
                @"mutation createComment($id: String!, $comment: String!) {
                    createComment(inputs: {clipId: $id, comment: $comment}) {
                        clip {
                            ... clipDetailFragment
                        }
                    }
                }";
            var res = client.execute<CreateCommentResult>(
                    query + clipDetailFragment,
                    "id", clipId,
                    "comment", comment
                    );
            updateClipDetails(res.createComment.clip);
        }

        private ClipDownloadDetails getDownloadDetails(string clipId) {
            const string query =
                @"query getDownloadUri($id: ID!) {
                    node(id: $id) {
                        ... on MocapClip {
                            originalDataDownloadUri
                                originalFileName
                        }
                    }
                }";
            var res = client.execute<GetDownloadUriResult>(query, "id", clipId);
            return res.node;
        }

        private void updateClipDetails(ClipDetail details) {
            this.clipDetails = details;

            if (clipDetails.previewImageUri != null) {
                getPreviewImage(clipDetails.previewImageUri);
            } else {
                this.clipPreviewImage = null;
            }
        }

        private void getPreviewImage(string uri) {
            var req = UnityWebRequest.Get(uri);
            Utils.waitForWebRequest(req);
            var data = req.downloadHandler.data;

            this.clipPreviewImage = new Texture2D(128,128);
            this.clipPreviewImage.LoadImage(data);
        }

        private void downloadAsset(string clipId) {
            var details = getDownloadDetails(clipId);

            var path = EditorUtility.SaveFilePanelInProject(
                    "Save motion data",
                    details.originalFileName, "fbx",
                    "Please enter a file name to save the motion data to");

            if (path.Length != 0) {
                var req = UnityWebRequest.Get(details.originalDataDownloadUri);
                Utils.waitForWebRequest(req);
                File.WriteAllBytes(path, req.downloadHandler.data);
                AssetDatabase.Refresh();
            }
        }


        [MenuItem("Moveshelf/Search animation")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            var window = EditorWindow.GetWindow(typeof(SearchTool), false, "Moveshelf");
            window.Show();
        }

        [MenuItem("Moveshelf/moveshelf.com")]
        static void Open()
        {
            Application.OpenURL("https://moveshelf.com/explore");
        }

        void OnEnable()
        {
            //Load API from Environment Variables, if present
            myApiKey = EditorPrefs.GetString(MVSHLF_API_KEY);
            client = new GraphQl.Client(myApiKey);
        }

        void OnGUI()
        {
            var width = position.width - 20;
            EditorGUIUtility.labelWidth = LABEL_WIDTH;

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.BeginHorizontal();
            myApiKey = EditorGUILayout.PasswordField("API KEY", myApiKey);
            if (GUILayout.Button("Set API Key", GUILayout.Width(BUTTON_WIDTH)))
            {
                EditorPrefs.SetString(MVSHLF_API_KEY, myApiKey);
                client = new GraphQl.Client(myApiKey);
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Label("Quick search", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            myString = EditorGUILayout.TextField("Keywords", myString);
            if (GUILayout.Button("SEARCH", GUILayout.Width(BUTTON_WIDTH)))
            {
                search(myString);
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Label("Search results", EditorStyles.boldLabel);
            if (searchResults.mocapClips.edges != null) {
                foreach (var clip in searchResults.mocapClips.edges) {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(clip.node.title, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(width - 2*BUTTON_WIDTH));
                    if (GUILayout.Button("import", GUILayout.Width(BUTTON_WIDTH)))
                    {
                        downloadAsset(clip.node.id);
                    }
                    if (GUILayout.Button("preview", GUILayout.Width(BUTTON_WIDTH)))
                    {
                        getClipDetails(clip.node.id);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            GUILayout.Label("Clip info", EditorStyles.boldLabel);
            if (clipDetails.id != null) {
                GUILayout.Label("Title: " + clipDetails.title);
                GUILayout.Label("Description: " + clipDetails.description);
                if (GUILayout.Button("See it on Moveshelf.com", GUILayout.Width(width))) {
                    Application.OpenURL("https://moveshelf.com/edit/"+clipDetails.id);
                }

                GUILayout.Label("Comments", EditorStyles.boldLabel);
                foreach (var edge in clipDetails.comments.edges) {
                    var comment = edge.node.comment;
                    var author = edge.node.author.displayName;
                    GUILayout.Label(author + ": " + comment);
                }

                myComment = EditorGUILayout.TextField("Comment: ", myComment);
                if (GUILayout.Button("Send", GUILayout.Width(width))) {
                    createComment(clipDetails.id, myComment);
                }

                GUILayout.Label("Preview image", EditorStyles.boldLabel);
                if (this.clipPreviewImage != null) {
                    GUILayout.Label(this.clipPreviewImage, GUILayout.MaxWidth(width));
                } else {
                    GUILayout.Label("No preview available");
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
