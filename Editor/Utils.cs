using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
using LitJson;

namespace Moveshelf {
    class Utils {
        public static void waitForWebRequest(UnityWebRequest request) {
            request.SendWebRequest();

            while (!request.isDone)
            {
                EditorUtility.DisplayProgressBar("Fetching data...", "Please wait while your request is completed!", request.downloadProgress / 1);
            };

            EditorUtility.ClearProgressBar();

            if (request.isNetworkError) {
                Debug.LogError("Network error encountered while executing API request");
            }
            if (request.isHttpError) {
                Debug.LogErrorFormat("Error executing HTTP request. Status code: {0}", request.responseCode);
            }
        }
    }

    namespace GraphQl {
        [Serializable()]
        public struct ErrorLocation {
            public int line;
            public int column;
        }

        [Serializable()]
        public struct Error {
            public string message;
            public ErrorLocation[] locations;
        }


        [Serializable()]
        public class ErrorException: System.Exception {
            public string query;
            public Error[] errors;

            public ErrorException(): base() {}
            public ErrorException(string message): base(message) {}
            public ErrorException(string message, System.Exception inner): base(message, inner) {}
            protected ErrorException(System.Runtime.Serialization.SerializationInfo info,
                    System.Runtime.Serialization.StreamingContext context) {}

            public ErrorException(string query, Error[] errors) {
                this.query = query;
                this.errors = errors;
            }

            public override string ToString() {
                var sb = new StringBuilder();
                foreach(var err in errors) {
                    sb.AppendFormat("GraphQL Error: {0} at line {1} column {2}", err.message, err.locations[0].line, err.locations[0].column);
                    sb.AppendLine();
                }
                return sb.ToString();
            }
        }

        public struct Result<T> {
            public T data;
            public Error[] errors;
        }

        class Client {
            private string apiKey;

            public Client(string apiKey) {
                this.apiKey = apiKey;
            }

            public T execute<T>(string query, params string[] vars) {
                using (var req = new UnityWebRequest("https://api.moveshelf.com/graphql", "POST"))
                {
                    byte[] data = buildGraphQlPayload(query, vars);
                    req.uploadHandler = (UploadHandlerRaw)new UploadHandlerRaw(data);
                    req.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

                    req.SetRequestHeader("Authorization", "Bearer " + this.apiKey);
                    req.SetRequestHeader("Content-Type", "application/json");
                    req.chunkedTransfer = false;
                    Utils.waitForWebRequest(req);

                    var result = JsonMapper.ToObject<Result<T>>(req.downloadHandler.text);
                    if (result.errors != null) {
                        var err = new ErrorException(query, result.errors);
                        Debug.LogWarning(err);
                        throw err;

                    }
                    return result.data;
                }
            }

            private byte[] buildGraphQlPayload(string query, params string[] vars) {
                Debug.Assert(vars.Length % 2 == 0, "Variables and values should be provided in pairs");

                var sb = new System.Text.StringBuilder();
                var writer = new JsonWriter(sb);
                writer.WriteObjectStart();
                writer.WritePropertyName("query");
                writer.Write(query);
                writer.WritePropertyName("variables");
                writer.WriteObjectStart();
                for (int i = 0; i < vars.Length; i+=2) {
                    writer.WritePropertyName(vars[i]);
                    writer.Write(vars[i+1]);
                }
                writer.WriteObjectEnd();
                writer.WriteObjectEnd();

                return Encoding.UTF8.GetBytes(sb.ToString());
            }
        }
    }

    namespace Relay {
        public struct Edge<T> {
            public T node;
        }

        public struct Connection<T> {
            public Edge<T>[] edges;
        }
    }
}
