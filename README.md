Note: Only the three projects Routing, RoutingCli and RoutingApi are relevant for external users. The rest are for internal use, and can safely be removed from the solution.

# Routing

Efficient library for finding the best route between two points in a (road) network. Uses Dijkstra. RoutingCLI and RoutingApi are two different wrappers around the library, superfluously documented below.

# RoutingCli

Command line interface for finding routes between one or more coordinate pairs.

Warning: must be run with custom binary encoded road network file, which can be downloaded from here: http://mobilitet.sintef.no/energimodul/roadnetworks/

Run RoutingCli.exe "C:\path\to\config.json".

Example config:

    {
	    "searches": [
            {
                "source": [
                    242541.20553505284,
                    7021740.248120374
                ],
                "target": [
                    253416.78153408598,
                    7131740.248120374
                ]
            },
            {
                "source": [
                    253416.78153408598,
                    7030691.380750257
                ],
                "target": [
                    242541.20553505284,
                    7030621.380750257
                ]
            },
            {
                "source": [
                    267243.5700083785,
                    6764480.177584507
                ],
                "target": [
                    262640.4187730937,
                    6711480.177584507
                ]
            }
        ],
	    "networkPath": "vegnettRuteplan_FGDB_20210528_tolerance-0001.bin",
	    "resultsPath": "results.json"
    }


# RoutingApi

Simple .NET REST API for finding routes between one or more coordinate pairs.

Warning: must be run with custom binary encoded road network file, which can be downloaded from here: http://mobilitet.sintef.no/energimodul/roadnetworks/

Start it using Visual Studio/IIS, and use it by sending POST requests with a list of coordinates (at least two). The API will handle any extra coordinates as waypoints.

Make sure to update the road network file location in appsettings.json.

Returns an object containing a list of WGS84 coordinates for the best route, as well as road link references that can be used to fetch more road data from the road network.

Note: the road network will be loaded into memory on the first request. The first request will therefore take longer time than subsequent requests.

Example POST request:

    curl --location --request POST 'http://localhost:49512/api/routing' \
    --header 'Content-Type: application/json' \
    --data-raw '[{"lat":63.413602,"lng":10.412028},{"lat":63.39914744974852,"lng":10.35038694326308}]'
