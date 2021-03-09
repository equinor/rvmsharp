cmake_minimum_required(VERSION 3.14.5)

project (tessbridge LANGUAGES CXX C VERSION 0.0.1)

#if (MSVC)
#    foreach(flag_var
#            CMAKE_CXX_FLAGS CMAKE_CXX_FLAGS_DEBUG CMAKE_CXX_FLAGS_RELEASE
#            CMAKE_CXX_FLAGS_MINSIZEREL CMAKE_CXX_FLAGS_RELWITHDEBINFO)
#        if(${flag_var} MATCHES "/MD")
#            string(REGEX REPLACE "/MD" "/MT" ${flag_var} "${${flag_var}}")
#        endif(${flag_var} MATCHES "/MD")
#    endforeach()
#endif()

set(TARGET_SOURCES
    src/tessbridge.cpp
)

set(LIBTESS2_SOURCES
    ../libtess2/Source/bucketalloc.c
    ../libtess2/Source/dict.c
    ../libtess2/Source/geom.c
    ../libtess2/Source/mesh.c
    ../libtess2/Source/priorityq.c
    ../libtess2/Source/sweep.c
    ../libtess2/Source/tess.c
)

include_directories(
    include
    ../libtess2/Include
)

add_library(tess2 STATIC ${LIBTESS2_SOURCES})
add_library(tessbridge SHARED ${TARGET_SOURCES})
target_link_libraries(tessbridge tess2)
set_property(TARGET tessbridge PROPERTY CXX_STANDARD 14)
target_compile_definitions(tessbridge PUBLIC TESSBRIDGE_BUILD_AS_DLL)
