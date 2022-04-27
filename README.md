# Routing

Efficient library for finding the best route between two points in a (road) network. Uses Dijkstra. RoutingCLI and RoutingApi are two different wrappers around the library, superfluously documented below.

# RoutingCLI

Command line interface for finding routes between one or more coordinate pairs.

Warning: must be run with custom binary encoded road network file, which can be downloaded from here: http://mobilitet.sintef.no/energimodul/roadnetworks/

Run RoutingCLI.exe "C:\path\to\config.json".

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

Make sure to update the road network file location in web.config.

Returns an object containing a list of WGS84 coordinates for the best route, as well as road link references that can be used to fetch more road data from the road network.

Example POST request:

    curl --location --request POST 'http://localhost:49512/api/routing' \
    --header 'Content-Type: application/json' \
    --data-raw '[{"lat":63.413602,"lng":10.412028},{"lat":63.39914744974852,"lng":10.35038694326308}]'
