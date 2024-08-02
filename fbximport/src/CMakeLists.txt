set(SOURCES
    common.h
    node.h
    node.cpp
    mesh.h
    mesh.cpp
    manager.h
    manager.cpp
    importer.h
    importer.cpp
)

if(APPLE)
    find_library(COREFOUNDATION_FRAMEWORK CoreFoundation)
    find_library(ICONV_LIBRARY iconv)

    set(LIBRARY_DEPENDENCIES ${FBX_REQUIRED_LIBS_DEPENDENCY} ${COREFOUNDATION_FRAMEWORK} ${ICONV_LIBRARY})
else()
    set(LIBRARY_DEPENDENCIES ${FBX_REQUIRED_LIBS_DEPENDENCY})
endif()

add_library(cfbx SHARED ${SOURCES})
target_link_libraries(cfbx PRIVATE ${LIBRARY_DEPENDENCIES} )
set_property(TARGET cfbx PROPERTY CXX_STANDARD 20)

target_compile_definitions(cfbx PUBLIC FBXSDK_VERSION="${FBXSDK_VERSION}" CFBX_BUILD_AS_DLL)
