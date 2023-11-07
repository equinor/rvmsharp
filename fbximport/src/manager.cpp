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

std::string* get_fbxsdk_version()
{
#ifdef FBXSDK_VERSION
    std::string* fbxVers = DBG_NEW std::string(FBXSDK_VERSION);
    return fbxVers;
#else
    std::string* fbxVers = DBG_NEW std::string("-1");
#endif
    return fbxVers;
}

void delete_fbxsdk_version(std::string* fbxVers)
{
    if (fbxVers == nullptr)
        return;

    delete fbxVers;
}