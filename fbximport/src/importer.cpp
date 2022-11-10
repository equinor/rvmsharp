#include "importer.h"
#include <fbxsdk.h>
#include <iostream>

using namespace fbxsdk;
using namespace std;

void* load_file(const char* filename, void* sdk)
{
    FbxManager* lSdkManager = (FbxManager*)sdk;

    // Setup IO settings
    FbxIOSettings* ios = FbxIOSettings::Create(lSdkManager, IOSROOT);
    ios->SetBoolProp(IMP_FBX_MATERIAL, false);
    ios->SetBoolProp(IMP_FBX_TEXTURE, false);
    ios->SetBoolProp(IMP_FBX_LINK, false);
    ios->SetBoolProp(IMP_FBX_SHAPE, false);
    ios->SetBoolProp(IMP_FBX_GOBO, false);
    ios->SetBoolProp(IMP_FBX_ANIMATION, false);
    ios->SetBoolProp(IMP_FBX_GLOBAL_SETTINGS, false);
    lSdkManager->SetIOSettings(ios);

    FbxImporter* lImporter = FbxImporter::Create(lSdkManager, "");
    if (!lImporter->Initialize(filename, -1, lSdkManager->GetIOSettings()))
    {
        cerr << "Call to FbxImporter::Initialize() failed." << endl;
        cerr << "Error returned: " << lImporter->GetStatus().GetErrorString() << endl;
        lSdkManager->Destroy();
        exit(-1);
    }

    FbxScene* lScene = FbxScene::Create(lSdkManager, "myScene");
    lImporter->Import(lScene);

    //FbxAxisSystem as(FbxAxisSystem::eYAxis, (FbxAxisSystem::EFrontVector)FbxAxisSystem::eParityEven, FbxAxisSystem::eLeftHanded);
    //as.ConvertScene(lScene);

    FbxNode* lRoot = lScene->GetRootNode();

    return lRoot;
}