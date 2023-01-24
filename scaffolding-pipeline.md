# TODO RvmSharp
- sample environment
  - input files to test with (TestSamples to maybe start with)
- pws script: pipeline
   - input params:
      - folder path to input files
      - folder path for the output files
   - steps: 
      - find .fbx (3D model), .csv (attributes), .json (metadata) in the folder
      - parse metadata (get modelID)
      - read versionNr from .json (metadata)
      - delete content of both WorkDirectory and OutputDirectory (set of sector files can be different for different versions of the model)
      - start CadRevealComposer.exe and run the pipeline
      - rename hierarchy.db to expected_hierarchyID.db
      - write a metadata file .json for the later upload to Echo Model Distribution Service

# TODO ModelDistribution
- put a filter on project section (when the list of candidates for a main models is received, scaffolding should filtered away)
- expose the `metadata` field in the model list, so we can use it for showing scaffolding metadata  (and other "non-standardized metadata")
# TODO 3dWeb
- selection of a scaffolding model should get metadata from the scaffolding model and not from the main model
- switching of main model must unload all additional models (scaffolding) that were loaded
- Add a TEMP UI to load scaffolding into 3D model on demand.
   - Button/Switch to load/unload a scaffolding model (ala the Model list)
   - Some basic metadata
   - Add a "jump to" button.
   - OOOOPTIONAL!: Consider adding "hihglighting etc" to highlight a specififct scaffold

   