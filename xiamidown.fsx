#r "System.Xml.dll"
#r "System.Xml.Linq.dll"

open System
open System.Threading
open System.IO
open System.Net
open System.Xml.Linq

let INPUT_FILE = "input.txt"
let XIAMI_PLAYLIST_URI = "http://www.xiami.com/song/playlist/id/{0}/type/3"
let DOWNFILE_PARALLEL_LIMIT = 5

let limitedParallel (n : int) (tseq : Async<'a> seq) =
    let ts = Seq.toArray tseq
    let rets = Array.zeroCreate ts.Length
    let count = ref -1
    let rec worker wid =
        async {
            let c = Interlocked.Increment count
            if c < ts.Length then
                let! ret = ts.[c]
                rets.[c] <- ret
                do! worker wid
        }
    async {
        let ws = Array.init n worker
        do! ws |> Async.Parallel |> Async.Ignore
        return rets
    }

let decodeLocation(location : String) =
    let n = Convert.ToInt32(location.[0]) - Convert.ToInt32('0')
    let l = location.Length - 1
    let d, m = l / n, l % n
    seq {
        for i in [| 0 .. d |] do
            for j in [| 0 .. n - 1 |] do
                yield location.[d * j + Math.Min(j, m) + i + 1] 
    }
    |> Seq.take(l)
    |> Seq.toArray
    |> fun cs -> new String(cs)
    |> Uri.UnescapeDataString
    |> fun s -> s.Replace('^', '0')

let downFile(uri : Uri, file : string) =
    async {
        if File.Exists(file) then 
            Console.Error.WriteLine("File '{0}' already exist, download canceled.", file)
        else
            let req = WebRequest.Create(uri)
            let! resp = req.AsyncGetResponse()
            use stream = resp.GetResponseStream()
            stream.CopyTo(File.OpenWrite(file))
            Console.Error.WriteLine("File '{0}' downloaded.", file)
    }

let downTrack(track : XElement) =
    seq {
        let ns = track.GetDefaultNamespace()
        let title = track.Element(ns.GetName "title").Value

        let location = track.Element(ns.GetName "location").Value
        yield downFile(new Uri(decodeLocation location), title + ".mp3")

        let lyric = track.Element(ns.GetName "lyric").Value
        if lyric <> "" then
            yield downFile(new Uri(lyric), title + ".lrc")
    }

let downPlaylist (stream : Stream) =
    let d = XDocument.Load stream
    let ns = d.Root.GetDefaultNamespace()
    d.Root.Element(ns.GetName "trackList").Elements(ns.GetName "track")
    |> Seq.map downTrack
    |> Seq.concat
    |> limitedParallel DOWNFILE_PARALLEL_LIMIT
    |> Async.Ignore

let job (uri : Uri) =
    async {
        Console.Error.WriteLine("Playlist '{0}'", uri.ToString())
        let req = WebRequest.Create(uri)
        let! resp = req.AsyncGetResponse()
        use stream = resp.GetResponseStream()
        do! downPlaylist stream
    }

let ids = File.ReadAllLines(INPUT_FILE)

for id in ids do
    if id <> "" then
        use writer = new StringWriter()
        writer.Write(XIAMI_PLAYLIST_URI, id)
        let uri = new Uri (writer.ToString())
        job uri |> Async.RunSynchronously

Console.Error.WriteLine("Finish!!")
