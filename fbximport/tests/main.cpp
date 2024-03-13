#include "tests.h"
#include <iostream>
#include <catch2/catch_all.hpp>
#include "fbx_info.h"

using namespace std;

int invoke_catch2_tests(int argc, char* argv[])
{
    Catch::Session session;

    std::string filePath = "AQ110South-3DView.fbx";

    // Build a new parser on top of Catch2's
    using namespace Catch::Clara;
    auto cli = session.cli()                // Get Catch2's command line parser
    | Opt(filePath, "modelfile")            // bind variable to a new option, with a hint string
        ["-m"]["--modelfile"]               // the option names it will respond to
        ("Model file path (fbx-file)");     // description string for the help output

    // Now pass the new composite back to Catch2 so it uses that
    session.cli(cli);

    // Let Catch2 (using Clara) parse the command line
    int returnCode = session.applyCommandLine(argc, argv);
    if( returnCode != 0 )
        return returnCode;

    // If set on the command line then the model file path is now set at this point
    set_test_model_file_path(filePath);

    return session.run();
}

int main(int argc, char* argv[])
{
    invoke_catch2_tests(argc, argv);
    return 0;
}
