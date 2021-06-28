using System;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using System.Numerics;
using System.Globalization;
using System.Collections.Generic;

namespace CoreEngine.Tools.Compiler.ResourceCompilers.Meshes
{
    struct FaceElement
    {
        public int VertexIndex;
        public int TextureCoordinatesIndex;
        public int NormalIndex;
    }

    public class ObjMeshDataReader : MeshDataReader
    {
        private bool invertHandedness = true;

        public override Task<ImportMeshData?> ReadAsync(ReadOnlyMemory<byte> sourceData)
        {
            // TODO: Use Disney osOcean.obj to test performances
            // FIXME: Disney obj files use Catmull-Clark subdivision surfaces

            var result = new ImportMeshData();

            var vertexDictionary = new Dictionary<MeshVertex, uint>();
            var vertexList = new List<Vector3>();
            var vertexNormalList = new List<Vector3>();
            var vertexTextureCoordinatesList = new List<Vector3>();

            // MeshSubObject? currentSubObject = null;

            // currentSubObject = new MeshSubObject();
            //             currentSubObject.StartIndex = (uint)result.Indices.Count;
            //             vertexDictionary.Clear();

            // TODO: Try to avoid the ToArray call that copy the buffer to the MemoryStream
            using var reader = new StreamReader(new MemoryStream(sourceData.ToArray()));
            
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine()!;

                // TODO: Wait for the Span<char> split method that is currenctly in dev
                var lineParts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (lineParts.Length > 1)
                {
                    if (lineParts[0] == "g")
                    {
                        // if (currentSubObject != null)
                        // {
                        //     currentSubObject.IndexCount = (uint)result.Indices.Count - currentSubObject.StartIndex;
                            
                        //     if (currentSubObject.IndexCount > 0)
                        //     {
                        //         result.MeshSubObjects.Add(currentSubObject);
                        //     }
                        // }
                    }

                    if (lineParts[0] == "v")
                    {
                        ParseVectorElement(vertexList, line.AsSpan());
                    }

                    else if (lineParts[0] == "vn")
                    {
                        ParseVectorElement(vertexNormalList, line.AsSpan());
                    }

                    else if (lineParts[0] == "vt")
                    {
                        ParseVectorElement(vertexTextureCoordinatesList, line.AsSpan());
                    }

                    else if (lineParts[0] == "f")
                    {
                        ParseFace(result, vertexDictionary, vertexList, vertexNormalList, vertexTextureCoordinatesList, line.AsSpan());
                    }

                    // else if (currentSubObject != null && lineParts[0] == "usemtl")
                    // {
                    //     if (((uint)result.Indices.Count - currentSubObject.StartIndex) > 0)
                    //     {
                    //         if (currentSubObject != null)
                    //         {
                    //             currentSubObject.IndexCount = (uint)result.Indices.Count - currentSubObject.StartIndex;
                                
                    //             if (currentSubObject.IndexCount > 0)
                    //             {
                    //                 result.MeshSubObjects.Add(currentSubObject);
                    //             }
                    //         }

                    //         currentSubObject = new MeshSubObject();
                    //         currentSubObject.StartIndex = (uint)result.Indices.Count;
                    //         vertexDictionary.Clear();

                    //         currentSubObject.MaterialPath = lineParts[1];
                    //     }

                    //     else
                    //     {
                    //         currentSubObject.MaterialPath = lineParts[1];
                    //     }
                    // }
                }
            }

            // if (currentSubObject != null)
            // {
            //     currentSubObject.IndexCount = (uint)result.Indices.Count - currentSubObject.StartIndex;

            //     if (currentSubObject.IndexCount > 0)
            //     {
            //         result.MeshSubObjects.Add(currentSubObject);
            //     }
            // }

            return Task.FromResult<ImportMeshData?>(result);
        }

        private void ParseVectorElement(List<Vector3> vectorList, ReadOnlySpan<char> line)
        {
            // TODO: Wait for the Span<char> split method that is currenctly in dev

            var stringLine = line.ToString();
            var lineParts = stringLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (lineParts.Length < 3)
            {
                throw new InvalidDataException("Invalid obj vertor line");
            }

            var x = float.Parse(lineParts[1], CultureInfo.InvariantCulture);
            var y = float.Parse(lineParts[2], CultureInfo.InvariantCulture);
            var z = 0.0f;
            
            if (lineParts.Length > 3)
            {
                z = float.Parse(lineParts[3], CultureInfo.InvariantCulture);
            }

            if (this.invertHandedness)
            {
                z = -z;
            }

            vectorList.Add(new Vector3(x, y , z));
        }

        private void ParseFace(ImportMeshData meshData, Dictionary<MeshVertex, uint> vertexDictionary, List<Vector3> vertexList, List<Vector3> vertexNormalList, List<Vector3> vertexTextureCoordinatesList, ReadOnlySpan<char> line)
        {
            // TODO: Wait for the Span<char> split method that is currenctly in dev

            var stringLine = line.ToString();
            var lineParts = stringLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (lineParts.Length < 4)
            {
                throw new InvalidDataException("Invalid obj vertex line");
            }

            var element1 = ParceFaceElement(lineParts[1]);
            var element2 = ParceFaceElement(lineParts[2]);
            var element3 = ParceFaceElement(lineParts[3]);

            if (!this.invertHandedness)
            {
                AddFaceElement(meshData, vertexDictionary, vertexList, vertexNormalList, vertexTextureCoordinatesList, element1);
                AddFaceElement(meshData, vertexDictionary, vertexList, vertexNormalList, vertexTextureCoordinatesList, element2);
                AddFaceElement(meshData, vertexDictionary, vertexList, vertexNormalList, vertexTextureCoordinatesList, element3);
            }

            else 
            {
                AddFaceElement(meshData, vertexDictionary, vertexList, vertexNormalList, vertexTextureCoordinatesList, element1);
                AddFaceElement(meshData, vertexDictionary, vertexList, vertexNormalList, vertexTextureCoordinatesList, element3);
                AddFaceElement(meshData, vertexDictionary, vertexList, vertexNormalList, vertexTextureCoordinatesList, element2);
            }

            if (lineParts.Length == 5)
            {
                var element4 = ParceFaceElement(lineParts[4]);

                if (!this.invertHandedness)
                {
                    AddFaceElement(meshData, vertexDictionary, vertexList, vertexNormalList, vertexTextureCoordinatesList, element1);
                    AddFaceElement(meshData, vertexDictionary, vertexList, vertexNormalList, vertexTextureCoordinatesList, element3);
                    AddFaceElement(meshData, vertexDictionary, vertexList, vertexNormalList, vertexTextureCoordinatesList, element4);
                }

                else
                {
                    AddFaceElement(meshData, vertexDictionary, vertexList, vertexNormalList, vertexTextureCoordinatesList, element1);
                    AddFaceElement(meshData, vertexDictionary, vertexList, vertexNormalList, vertexTextureCoordinatesList, element4);
                    AddFaceElement(meshData, vertexDictionary, vertexList, vertexNormalList, vertexTextureCoordinatesList, element3);
                }
            }
        }

        private static void AddFaceElement(ImportMeshData meshData, Dictionary<MeshVertex, uint> vertexDictionary, List<Vector3> vertexList, List<Vector3> vertexNormalList, List<Vector3> vertexTextureCoordinatesList, FaceElement faceElement)
        {
            var vertex = ConstructVertex(vertexList, vertexNormalList, vertexTextureCoordinatesList, faceElement);

            if (!vertexDictionary.ContainsKey(vertex))
            {
                meshData.Indices.Add((uint)meshData.Vertices.Count);
                vertexDictionary.Add(vertex, (uint)meshData.Vertices.Count);
                meshData.Vertices.Add(vertex);
            }

            else
            {
                var vertexIndex = vertexDictionary[vertex];
                meshData.Indices.Add((uint)vertexIndex);
            }
        }

        private static FaceElement ParceFaceElement(string faceElement)
        {
            var result = new FaceElement();

            var faceElements = faceElement.Split('/');

            result.VertexIndex = int.Parse(faceElements[0], CultureInfo.InvariantCulture);

            if (faceElements.Length > 1 && !string.IsNullOrEmpty(faceElements[1]))
            {
                result.TextureCoordinatesIndex = int.Parse(faceElements[1], CultureInfo.InvariantCulture);
            }

            if (faceElements.Length > 2 && !string.IsNullOrEmpty(faceElements[2]))
            {
                result.NormalIndex = int.Parse(faceElements[2], CultureInfo.InvariantCulture);
            }

            return result;
        }
        
        private static MeshVertex ConstructVertex(List<Vector3> vertexList, List<Vector3> vertexNormalList, List<Vector3> vertexTextureCoordinatesList, FaceElement faceElement)
        {
            var position = Vector3.Zero;
            var normal = Vector3.Zero;
            var textureCoordinates = Vector2.Zero;

            if (faceElement.VertexIndex > 0)
            {
                position = vertexList[faceElement.VertexIndex - 1];
            }

            else if (faceElement.VertexIndex < 0)
            {
                position = vertexList[vertexList.Count + faceElement.VertexIndex];

            }

            if (faceElement.NormalIndex > 0)
            {
                normal = vertexNormalList[faceElement.NormalIndex - 1];
            }

            else if (faceElement.NormalIndex < 0)
            {
                normal = vertexNormalList[vertexNormalList.Count + faceElement.NormalIndex];
            }

            if (faceElement.TextureCoordinatesIndex > 0)
            {
                var textureCoordinatesVec3 = vertexTextureCoordinatesList[faceElement.TextureCoordinatesIndex - 1];
                textureCoordinates = new Vector2(textureCoordinatesVec3.X, -textureCoordinatesVec3.Y);
            }

            else if (faceElement.TextureCoordinatesIndex < 0)
            {
                var textureCoordinatesVec3 = vertexTextureCoordinatesList[vertexTextureCoordinatesList.Count + faceElement.TextureCoordinatesIndex];
                textureCoordinates = new Vector2(textureCoordinatesVec3.X, -textureCoordinatesVec3.Y);
            }

            return new MeshVertex(position, normal, textureCoordinates);
        }
    }
}