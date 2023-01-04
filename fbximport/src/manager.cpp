#include "manager.h"
#include <fbxsdk.h>

CFbxManager manager_create()
{
    auto manager = FbxManager::Create();
    return static_cast<CFbxManager>(manager);
}

void manager_destroy(CFbxManager manager)
{
    if (manager == nullptr)
        return;

    auto fbxManager = static_cast<FbxManager*>(manager);
    fbxManager->Destroy();
}