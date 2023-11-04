const puppeteer = require("puppeteer");
const express = require("express");

(async () => {
    const app = express();
    const port = process.env.PORT || 9876;

    // create a browser instance
    app.listen(port, "localhost", () => {
        console.log(`Example app listening on port ${port}!`);
        console.log(
            `Run http://localhost:${port}/screenshot?w=640&h=480&url=https://example.org/ in your browser.`
        );
    });

    app.get("/", (req, res) => res.send("Hello World!"));
    app.get("/screenshot", async (req, res) => {
        console.log("launching browser");
        const browser = await puppeteer.launch({
            headless: "new",
        });
        console.log("browser launched");

        const page = await browser.newPage();
        await page.setViewport({
            width: parseInt(req.query.w),
            height: parseInt(req.query.h),
        });
        console.log(
            `Generating screenshot for ${req.query.url} with ${req.query.w}x${req.query.h}`
        );
        await page.goto(req.query.url);
        
        const imageBuffer = await page.screenshot();
        await browser.close();

        console.log(
            `Screenshot generated, image size is ${imageBuffer.length} bytes.`
        );

        res.set("Content-Type", "image/png");
        res.set("Content-Length", imageBuffer.length);
        res.send(imageBuffer);
    });
})();
