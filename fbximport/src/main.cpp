#include <fbxsdk.h>
#include <iostream>
#include <map>
#include <vector>

using namespace fbxsdk;
using namespace std;

#if _WIN32
    #ifdef FBXIMPORT_BUILD_AS_DLL
    #define FBXIMPORT_API __declspec(dllexport)
    #else
    #define FBXIMPORT_API __declspec(dllimport)
    #endif
#else
#define FBXIMPORT_API
#endif

extern "C" {
    FBXIMPORT_API struct Transform {
        float posX;
        float posY;
        float posZ;
        float rotX;
        float rotY;
        float rotZ;
        float rotW;
        float scaleX;
        float scaleY;
        float scaleZ;
    };

    FBXIMPORT_API struct ExportableMesh {
        int vertex_count;
        int triangle_count;
        float* vertex_data;
        float* normal_data;
        int* triangle_data;
    };

    FBXIMPORT_API void* sdk_init();
    FBXIMPORT_API void sdk_destroy(void* lSdkManager);
    FBXIMPORT_API void* load_file(const char* filename, void* sdk);
    FBXIMPORT_API int get_child_count(void* node);
    FBXIMPORT_API void* get_child(int index, void* parent);
    FBXIMPORT_API void get_name(void* node, char* name_array, int buffer_size);
    FBXIMPORT_API void* get_geometry(void* node);
    FBXIMPORT_API void clean_mesh(void* mesh_data);
    FBXIMPORT_API ExportableMesh* get_geometry_data(void* geometry);
    FBXIMPORT_API void get_transform(void* node, Transform* transform_out);
}





void iterate_children(FbxNode* parent, int ident = 0)
{
    char* name = new char[512];
    get_name(parent, name, 512);
    for (int i = 0; i < ident; i++) cout << "\t";

    cout << ident << ": " << name << endl;
    delete[] name;
    
    for (int i = 0; i < get_child_count(parent); i++)
    {
        iterate_children((FbxNode*)get_child(i, parent), ident + 1);
    }
    auto geometry = (FbxMesh*)get_geometry(parent);
    if (geometry != nullptr)
    {
        ExportableMesh* data = nullptr;
        data = (ExportableMesh*)get_geometry_data(geometry);
        if (data == nullptr)
        {
            cerr << "Could not retreive geometry" << endl;
        }
        else {
            cout << "Vertex count: " << data->vertex_count << endl;
            cout << "Triangle count: " << data->triangle_count << endl;
        }

    }
}

void get_transform(void* node, Transform* transform_out)
{
    auto lNode = (FbxNode*)node;
    auto t = lNode->LclTranslation.Get();
    FbxQuaternion r;
    r.ComposeSphericalXYZ(lNode->LclRotation.Get());
    auto s = lNode->LclScaling.Get();

    transform_out->posX = t[0];
    transform_out->posY = t[1];
    transform_out->posZ = t[2];
    transform_out->rotX = r[0];
    transform_out->rotY = r[1];
    transform_out->rotZ = r[2];
    transform_out->rotW = r[3];
    transform_out->scaleX = s[0];
    transform_out->scaleX = s[1];
    transform_out->scaleX = s[2];
}

void* get_geometry(void* node) {
    auto attr = ((FbxNode*)node)->GetNodeAttribute();
    if (attr != nullptr && attr->GetAttributeType() == FbxNodeAttribute::eMesh)
    {
        FbxMesh* mesh = (FbxMesh*)attr;
        if (!mesh->IsTriangleMesh())
        {
            cerr << "Not a triangle MESH!" << endl;
            return nullptr;
        }
        return mesh;
    }
    else {
        return nullptr;
    }
}

ExportableMesh* get_geometry_data(void* geometry)
{
    auto mesh = (FbxMesh*)geometry;
    // TODO UVs

    auto polyCount = mesh->GetPolygonVertexCount();
    auto triangleArray = mesh->GetPolygonVertices();
    auto controlPointCount = mesh->GetControlPointsCount();

    vector<FbxVector4> lFbxVertices;
    vector<int> lFbxTriangles;

    map<int, vector<int>> lFbxToOutIndexLookup;

    vector<FbxVector4> lOutVertices;
    vector<FbxVector4> lOutNormals;
    vector<int> lOutTriangles;


    for (auto i = 0; i < controlPointCount; i++)
    {
        auto cp = mesh->GetControlPointAt(i);
        lFbxVertices.push_back(cp);
    }
    
    FbxArray<FbxVector4> lFbxNormals;
    mesh->GetPolygonVertexNormals(lFbxNormals); // TODO if fail
    
    for (auto i = 0; i < polyCount; i++)
    {
        const auto fbxVertexIndex = triangleArray[i];
        auto lVertex = lFbxVertices[fbxVertexIndex];
        auto lNormal = lFbxNormals[i];

        auto lookUpOutIndex = lFbxToOutIndexLookup.find(fbxVertexIndex);
        if (lookUpOutIndex != lFbxToOutIndexLookup.end())
        {
            bool found = false;
            auto outPossibleIndicies = (*lookUpOutIndex).second;
            for (const auto outIndex : outPossibleIndicies)
            {
                auto toCompareNormal = lOutNormals[outIndex];
                if (lNormal == toCompareNormal)
                {
                    lOutTriangles.push_back(outIndex);
                    found = true;
                    break;
                }
            }

            if (!found) {
                auto outIndex = (int)lOutVertices.size();
                lOutVertices.push_back(lVertex);
                lOutNormals.push_back(lNormal);
                lOutTriangles.push_back(outIndex);
                lFbxToOutIndexLookup[fbxVertexIndex].push_back(outIndex);
            }
            // compare normal
                // if equals, add an index to triangle array
                // if not equals, add new vertex with new normal and andd to triangle array
        }
        else
        {
            // add new normal and vertex and triangle
            auto newIndex = lOutVertices.size();
            vector<int> newVector;
            newVector.push_back(newIndex);
            lFbxToOutIndexLookup[fbxVertexIndex] = newVector;
            lOutVertices.push_back(lVertex);
            lOutNormals.push_back(lNormal);
            lOutTriangles.push_back(newIndex);
        }
    }

    if (lOutVertices.size() != lOutNormals.size())
    {
        cout << "Vertex count: " << lOutVertices.size() << endl;
        cout << "Normal count: " << lOutNormals.size() << endl;
        cerr << "Normal count is not equal to vertex count" << endl;
    }

    auto mesh_out_tmp = new ExportableMesh();
    mesh_out_tmp->triangle_count = lOutTriangles.size();
    mesh_out_tmp->vertex_count = lOutVertices.size();
    mesh_out_tmp->vertex_data = new float[lOutVertices.size() * 3];
    mesh_out_tmp->triangle_data = new int[lOutTriangles.size()];
    mesh_out_tmp->normal_data = new float[lOutVertices.size() * 3];

    for (int i = 0; i < lOutVertices.size(); i++)
    {
        mesh_out_tmp->vertex_data[i * 3] = (float)lOutVertices[i][0];
        mesh_out_tmp->vertex_data[i * 3 + 1] = (float)lOutVertices[i][1];
        mesh_out_tmp->vertex_data[i * 3 + 2] = (float)lOutVertices[i][2];
        mesh_out_tmp->normal_data[i * 3] = (float)lOutNormals[i][0];
        mesh_out_tmp->normal_data[i * 3 + 1] = (float)lOutNormals[i][1];
        mesh_out_tmp->normal_data[i * 3 + 2] = (float)lOutNormals[i][2];
    }
    for (int i = 0; i < lOutTriangles.size(); i++)
    {
        mesh_out_tmp->triangle_data[i] = lOutTriangles[i];
    }
    return mesh_out_tmp;

}

void clean_mesh(void* mesh_data)
{
    auto mesh_out_tmp = (ExportableMesh*)mesh_data;
    delete[] mesh_out_tmp->vertex_data;
    delete[] mesh_out_tmp->triangle_data;
    delete[] mesh_out_tmp->normal_data;
    delete mesh_out_tmp;
}

int main(int argc, char** argv)
{
    if (argc < 2)
    {
        cout << "Usage: " << argv[0] << " INPUT.FBX OUTPUT.BIN" << endl;
        return 1;
    }
    auto sdk = sdk_init();
    const char *lInputFbxFilename = argv[1];
    auto root = (FbxNode*)load_file(lInputFbxFilename, sdk);

    iterate_children(root);

    sdk_destroy(sdk);

    return 0;
}

void* sdk_init()
{
    FbxManager *lSdkManager = FbxManager::Create();
    return lSdkManager;
}

void sdk_destroy(void* lSdkManager)
{
    ((FbxManager*)lSdkManager)->Destroy();
}

int get_child_count(void* node)
{
    return ((FbxNode*)node)->GetChildCount();
}

void get_name(void* node, char* name_array, int buffer_size)
{
    const char* name = ((FbxNode*)node)->GetName();
    strcpy_s(name_array, buffer_size, name);
}

void* get_child(int index, void* parent) 
{
    return ((FbxNode*)parent)->GetChild(index);
}

void* load_file(const char* filename, void* sdk)
{
    FbxManager *lSdkManager = (FbxManager*)sdk;

    // Setup IO settings
    FbxIOSettings *ios = FbxIOSettings::Create(lSdkManager, IOSROOT);
    ios->SetBoolProp(IMP_FBX_MATERIAL,        false);
    ios->SetBoolProp(IMP_FBX_TEXTURE,         false);
    ios->SetBoolProp(IMP_FBX_LINK,            false);
    ios->SetBoolProp(IMP_FBX_SHAPE,           false);
    ios->SetBoolProp(IMP_FBX_GOBO,            false);
    ios->SetBoolProp(IMP_FBX_ANIMATION,       false);
    ios->SetBoolProp(IMP_FBX_GLOBAL_SETTINGS, false);
    lSdkManager->SetIOSettings(ios);

    

    FbxImporter *lImporter = FbxImporter::Create(lSdkManager, "");
    if (!lImporter->Initialize(filename, -1, lSdkManager->GetIOSettings()))
    {
        cerr << "Call to FbxImporter::Initialize() failed." << endl;
        cerr << "Error returned: " << lImporter->GetStatus().GetErrorString() << endl;
        lSdkManager->Destroy();
        exit(-1);
    }

    FbxScene *lScene = FbxScene::Create(lSdkManager, "myScene");
    lImporter->Import(lScene);

    //FbxAxisSystem as(FbxAxisSystem::eYAxis, (FbxAxisSystem::EFrontVector)FbxAxisSystem::eParityEven, FbxAxisSystem::eLeftHanded);
    //as.ConvertScene(lScene);

    FbxNode* lRoot = lScene->GetRootNode();

    return lRoot;
}