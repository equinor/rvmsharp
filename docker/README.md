### ci.pullreview.Dockerfile

The build will only be sucessful if all test pass sucessfully.

``` cmd
docker build --pull --progress=plain --file ci.pullreview.Dockerfile ../
```

### ci.publish.Dockerfile

Builds an image that ultimately produces a published build.

``` cmd
docker build --pull --progress=plain --file ci.publish.Dockerfile ../
```
